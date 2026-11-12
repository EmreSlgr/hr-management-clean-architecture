namespace HumanResources.Domain.Enums;

public enum UserRole
{
    Employee = 1,
    HRManager = 2,
    Admin = 3
}

public enum LeaveType
{
    Annual = 1,
    Sick = 2,
    Maternity = 3,
    Unpaid = 4
}

public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
