using HumanResources.Domain.Common;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;

namespace HumanResources.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public int EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public LeaveType LeaveType { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int TotalDays { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; } = LeaveStatus.Pending;

    public int? ApproverId { get; private set; }
    public Employee? Approver { get; private set; }

    public DateTime? DecisionAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private LeaveRequest() { } // EF Core

    private LeaveRequest(
        Employee employee,
        LeaveType leaveType,
        DateTime startDate,
        DateTime endDate,
        int totalDays,
        string reason)
    {
        EmployeeId = employee.Id;
        Employee = employee;
        LeaveType = leaveType;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        TotalDays = totalDays;
        Reason = reason;
        Status = LeaveStatus.Pending;
    }

    public static LeaveRequest Create(
        Employee employee,
        LeaveType leaveType,
        DateTime startDate,
        DateTime endDate,
        string reason)
    {
        if (employee == null) throw new ArgumentNullException(nameof(employee));

        if (endDate.Date < startDate.Date)
            throw new InvalidLeaveOperationException("End date cannot be earlier than start date.");

        int totalDays = (endDate.Date - startDate.Date).Days + 1;

        if (totalDays <= 0)
            throw new InvalidLeaveOperationException("Total leave days must be at least 1 day.");

        if (leaveType == LeaveType.Annual && employee.AnnualLeaveBalance < totalDays)
        {
            throw new InsufficientLeaveBalanceException(totalDays, employee.AnnualLeaveBalance);
        }

        return new LeaveRequest(employee, leaveType, startDate, endDate, totalDays, reason);
    }

    public void Approve(Employee approver)
    {
        if (approver == null) throw new ArgumentNullException(nameof(approver));

        if (approver.Role != UserRole.HRManager && approver.Role != UserRole.Admin)
        {
            throw new UnauthorizedApproverException(
                $"Employee '{approver.FullName}' with role '{approver.Role}' is not authorized to approve leave requests. Required role: HRManager or Admin.");
        }

        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidLeaveOperationException(
                $"Cannot approve a leave request that is already in '{Status}' status.");
        }

        Status = LeaveStatus.Approved;
        ApproverId = approver.Id;
        DecisionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (LeaveType == LeaveType.Annual && Employee != null)
        {
            Employee.DeductLeaveBalance(TotalDays);
        }
    }

    public void Reject(Employee approver, string rejectionReason)
    {
        if (approver == null) throw new ArgumentNullException(nameof(approver));

        if (approver.Role != UserRole.HRManager && approver.Role != UserRole.Admin)
        {
            throw new UnauthorizedApproverException(
                $"Employee '{approver.FullName}' with role '{approver.Role}' is not authorized to reject leave requests.");
        }

        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidLeaveOperationException(
                $"Cannot reject a leave request that is already in '{Status}' status.");
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new InvalidLeaveOperationException("Rejection reason must be provided.");
        }

        Status = LeaveStatus.Rejected;
        ApproverId = approver.Id;
        RejectionReason = rejectionReason;
        DecisionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(int requestingEmployeeId)
    {
        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidLeaveOperationException(
                $"Only pending leave requests can be cancelled. Current status: '{Status}'.");
        }

        if (EmployeeId != requestingEmployeeId)
        {
            throw new InvalidLeaveOperationException("You can only cancel your own leave requests.");
        }

        Status = LeaveStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
