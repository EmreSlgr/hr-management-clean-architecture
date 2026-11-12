using HumanResources.Domain.Common;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;

namespace HumanResources.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Employee;
    public DateTime HireDate { get; private set; }
    public int AnnualLeaveBalance { get; private set; } = 15;

    public int DepartmentId { get; private set; }
    public Department? Department { get; private set; }

    private readonly List<LeaveRequest> _leaveRequests = new();
    public IReadOnlyCollection<LeaveRequest> LeaveRequests => _leaveRequests.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Employee() { } // EF Core

    public Employee(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string jobTitle,
        UserRole role,
        int departmentId,
        DateTime hireDate,
        int annualLeaveBalance = 15)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name cannot be empty.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));

        FirstName = firstName;
        LastName = lastName;
        Email = email.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        JobTitle = jobTitle;
        Role = role;
        DepartmentId = departmentId;
        HireDate = hireDate;
        AnnualLeaveBalance = annualLeaveBalance;
    }

    public void DeductLeaveBalance(int days)
    {
        if (days <= 0)
            throw new InvalidLeaveOperationException("Deduction days must be greater than zero.");

        if (AnnualLeaveBalance < days)
            throw new InsufficientLeaveBalanceException(days, AnnualLeaveBalance);

        AnnualLeaveBalance -= days;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLeaveBalance(int days)
    {
        if (days <= 0)
            throw new InvalidLeaveOperationException("Days to add must be greater than zero.");

        AnnualLeaveBalance += days;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string jobTitle, int departmentId)
    {
        FirstName = firstName;
        LastName = lastName;
        JobTitle = jobTitle;
        DepartmentId = departmentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }
}
