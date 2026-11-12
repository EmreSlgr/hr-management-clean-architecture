using FluentAssertions;
using HumanResources.Domain.Entities;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;
using Xunit;

namespace HumanResources.UnitTests.Domain;

public class LeaveRequestTests
{
    private Employee CreateTestEmployee(int balance = 15, UserRole role = UserRole.Employee)
    {
        return new Employee(
            "Emre",
            "Salgır",
            "emre@example.com",
            "hash",
            "Developer",
            role,
            departmentId: 1,
            DateTime.UtcNow.AddYears(-1),
            balance
        );
    }

    [Fact]
    public void Create_WhenEndDateBeforeStartDate_ShouldThrowInvalidLeaveOperationException()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var start = DateTime.UtcNow.AddDays(5);
        var end = DateTime.UtcNow.AddDays(2); // earlier

        // Act
        var act = () => LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Summer vacation");

        // Assert
        act.Should().Throw<InvalidLeaveOperationException>()
            .WithMessage("*End date cannot be earlier than start date*");
    }

    [Fact]
    public void Create_WhenAnnualLeaveExceedsBalance_ShouldThrowInsufficientLeaveBalanceException()
    {
        // Arrange: 5 days balance, but requesting 10 days
        var employee = CreateTestEmployee(balance: 5);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(10); // 10 days

        // Act
        var act = () => LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Long break");

        // Assert
        act.Should().Throw<InsufficientLeaveBalanceException>()
            .WithMessage("*Insufficient leave balance. Requested: 10 days, Available: 5 days.*");
    }

    [Fact]
    public void Create_WhenSickLeaveExceedsAnnualBalance_ShouldSucceedBecauseSickLeaveIsNotDeductedFromAnnual()
    {
        // Arrange: Sick leave doesn't depend on annual leave balance
        var employee = CreateTestEmployee(balance: 2);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(5); // 5 days

        // Act
        var request = LeaveRequest.Create(employee, LeaveType.Sick, start, end, "Medical surgery");

        // Assert
        request.Should().NotBeNull();
        request.Status.Should().Be(LeaveStatus.Pending);
        request.TotalDays.Should().Be(5);
    }

    [Fact]
    public void Approve_WhenApproverIsRegularEmployee_ShouldThrowUnauthorizedApproverException()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var peerApprover = CreateTestEmployee(balance: 15, role: UserRole.Employee);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(3);
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Trip");

        // Act
        var act = () => request.Approve(peerApprover);

        // Assert
        act.Should().Throw<UnauthorizedApproverException>()
            .WithMessage("*not authorized to approve leave requests*");
    }

    [Fact]
    public void Approve_WhenApproverIsHRManager_ShouldApproveAndDeductEmployeeBalance()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var hrManager = CreateTestEmployee(balance: 20, role: UserRole.HRManager);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(5); // 5 days
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Summer holiday");

        // Act
        request.Approve(hrManager);

        // Assert
        request.Status.Should().Be(LeaveStatus.Approved);
        request.DecisionAt.Should().NotBeNull();
        request.ApproverId.Should().Be(hrManager.Id);
        employee.AnnualLeaveBalance.Should().Be(10); // 15 - 5 = 10
    }

    [Fact]
    public void Approve_WhenApproverIsAdmin_ShouldApproveAndDeductEmployeeBalance()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 10);
        var admin = CreateTestEmployee(balance: 30, role: UserRole.Admin);
        var start = DateTime.UtcNow.AddDays(2);
        var end = DateTime.UtcNow.AddDays(4); // 3 days
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Conference");

        // Act
        request.Approve(admin);

        // Assert
        request.Status.Should().Be(LeaveStatus.Approved);
        employee.AnnualLeaveBalance.Should().Be(7); // 10 - 3 = 7
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowInvalidLeaveOperationException()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var hrManager = CreateTestEmployee(balance: 20, role: UserRole.HRManager);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(3);
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Trip");
        request.Approve(hrManager);

        // Act
        var act = () => request.Approve(hrManager);

        // Assert
        act.Should().Throw<InvalidLeaveOperationException>()
            .WithMessage("*Cannot approve a leave request that is already in 'Approved' status*");
    }

    [Fact]
    public void Reject_WhenReasonIsMissing_ShouldThrowInvalidLeaveOperationException()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var hrManager = CreateTestEmployee(balance: 20, role: UserRole.HRManager);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(3);
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Trip");

        // Act
        var act = () => request.Reject(hrManager, "   ");

        // Assert
        act.Should().Throw<InvalidLeaveOperationException>()
            .WithMessage("*Rejection reason must be provided*");
    }

    [Fact]
    public void Reject_WhenValidHRManager_ShouldSetStatusToRejectedAndNotDeductBalance()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var hrManager = CreateTestEmployee(balance: 20, role: UserRole.HRManager);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(5);
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Trip");

        // Act
        request.Reject(hrManager, "Staffing shortage during sprint crunch.");

        // Assert
        request.Status.Should().Be(LeaveStatus.Rejected);
        request.RejectionReason.Should().Be("Staffing shortage during sprint crunch.");
        employee.AnnualLeaveBalance.Should().Be(15); // Balance unchanged
    }

    [Fact]
    public void Cancel_WhenCancelledByOwner_ShouldSetStatusToCancelled()
    {
        // Arrange
        var employee = CreateTestEmployee(balance: 15);
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(3);
        var request = LeaveRequest.Create(employee, LeaveType.Annual, start, end, "Trip");

        // Act
        request.Cancel(employee.Id);

        // Assert
        request.Status.Should().Be(LeaveStatus.Cancelled);
    }
}
