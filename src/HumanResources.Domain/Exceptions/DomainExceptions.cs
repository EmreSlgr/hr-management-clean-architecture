namespace HumanResources.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InsufficientLeaveBalanceException : DomainException
{
    public InsufficientLeaveBalanceException(int requestedDays, int currentBalance)
        : base($"Insufficient leave balance. Requested: {requestedDays} days, Available: {currentBalance} days.")
    {
    }
}

public class UnauthorizedApproverException : DomainException
{
    public UnauthorizedApproverException(string message) : base(message) { }
}

public class InvalidLeaveOperationException : DomainException
{
    public InvalidLeaveOperationException(string message) : base(message) { }
}
