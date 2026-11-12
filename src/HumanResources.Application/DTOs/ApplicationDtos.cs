using HumanResources.Domain.Enums;

namespace HumanResources.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    int Id,
    string FullName,
    string Email,
    string Role,
    string Token,
    int AnnualLeaveBalance
);

public record DepartmentDto(int Id, string Name, string Description, int EmployeeCount);

public record CreateDepartmentRequest(string Name, string Description);

public record EmployeeDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string JobTitle,
    string Role,
    int DepartmentId,
    string DepartmentName,
    DateTime HireDate,
    int AnnualLeaveBalance
);

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string JobTitle,
    UserRole Role,
    int DepartmentId,
    int AnnualLeaveBalance = 15
);

public record LeaveRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    int TotalDays,
    string Reason,
    string Status,
    int? ApproverId,
    string? ApproverName,
    DateTime? DecisionAt,
    string? RejectionReason,
    DateTime CreatedAt
);

public record CreateLeaveRequestInput(
    LeaveType LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string Reason
);

public record RejectLeaveInput(string Reason);
