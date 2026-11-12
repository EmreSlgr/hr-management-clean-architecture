using HumanResources.Application.Common.Interfaces;
using HumanResources.Application.DTOs;
using HumanResources.Domain.Entities;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HumanResources.Application.Services;

public interface ILeaveService
{
    Task<LeaveRequestDto> SubmitLeaveAsync(int employeeId, CreateLeaveRequestInput input, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> ApproveLeaveAsync(int leaveId, int approverId, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> RejectLeaveAsync(int leaveId, int approverId, string reason, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> CancelLeaveAsync(int leaveId, int employeeId, CancellationToken cancellationToken = default);
    Task<List<LeaveRequestDto>> GetMyLeavesAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<List<LeaveRequestDto>> GetAllLeavesAsync(LeaveStatus? status = null, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public class LeaveService : ILeaveService
{
    private readonly IApplicationDbContext _context;

    public LeaveService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequestDto> SubmitLeaveAsync(int employeeId, CreateLeaveRequestInput input, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found.");

        var leave = LeaveRequest.Create(
            employee,
            input.LeaveType,
            input.StartDate,
            input.EndDate,
            input.Reason);

        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(leave, employee.FullName, null);
    }

    public async Task<LeaveRequestDto> ApproveLeaveAsync(int leaveId, int approverId, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .FirstOrDefaultAsync(l => l.Id == leaveId, cancellationToken);

        if (leave == null)
            throw new KeyNotFoundException($"Leave request with ID {leaveId} not found.");

        var approver = await _context.Employees.FindAsync(new object[] { approverId }, cancellationToken);
        if (approver == null)
            throw new KeyNotFoundException($"Approver with ID {approverId} not found.");

        // Domain method handles role checking, status transition & balance deduction
        leave.Approve(approver);

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(leave, leave.Employee?.FullName ?? "", approver.FullName);
    }

    public async Task<LeaveRequestDto> RejectLeaveAsync(int leaveId, int approverId, string reason, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .FirstOrDefaultAsync(l => l.Id == leaveId, cancellationToken);

        if (leave == null)
            throw new KeyNotFoundException($"Leave request with ID {leaveId} not found.");

        var approver = await _context.Employees.FindAsync(new object[] { approverId }, cancellationToken);
        if (approver == null)
            throw new KeyNotFoundException($"Approver with ID {approverId} not found.");

        // Domain method handles role checking & state transition
        leave.Reject(approver, reason);

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(leave, leave.Employee?.FullName ?? "", approver.FullName);
    }

    public async Task<LeaveRequestDto> CancelLeaveAsync(int leaveId, int employeeId, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == leaveId, cancellationToken);

        if (leave == null)
            throw new KeyNotFoundException($"Leave request with ID {leaveId} not found.");

        leave.Cancel(employeeId);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(leave, leave.Employee?.FullName ?? "", null);
    }

    public async Task<List<LeaveRequestDto>> GetMyLeavesAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var leaves = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return leaves.Select(l => MapToDto(l, l.Employee?.FullName ?? "", l.Approver?.FullName)).ToList();
    }

    public async Task<List<LeaveRequestDto>> GetAllLeavesAsync(LeaveStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var leaves = await query.OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken);
        return leaves.Select(l => MapToDto(l, l.Employee?.FullName ?? "", l.Approver?.FullName)).ToList();
    }

    public async Task<LeaveRequestDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.Approver)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (leave == null)
            throw new KeyNotFoundException($"Leave request with ID {id} not found.");

        return MapToDto(leave, leave.Employee?.FullName ?? "", leave.Approver?.FullName);
    }

    private static LeaveRequestDto MapToDto(LeaveRequest leave, string employeeName, string? approverName)
    {
        return new LeaveRequestDto(
            leave.Id,
            leave.EmployeeId,
            employeeName,
            leave.LeaveType.ToString(),
            leave.StartDate,
            leave.EndDate,
            leave.TotalDays,
            leave.Reason,
            leave.Status.ToString(),
            leave.ApproverId,
            approverName,
            leave.DecisionAt,
            leave.RejectionReason,
            leave.CreatedAt
        );
    }
}
