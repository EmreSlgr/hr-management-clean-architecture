using FluentAssertions;
using HumanResources.Domain.Entities;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;
using Xunit;

namespace HumanResources.UnitTests.Domain;

public class EmployeeTests
{
    private Employee CreateSampleEmployee(int initialBalance = 15)
    {
        return new Employee(
            "Emre",
            "Salgır",
            "emre@company.com",
            "passwordHash123",
            "Senior Software Developer",
            UserRole.Employee,
            departmentId: 1,
            DateTime.UtcNow.AddYears(-2),
            annualLeaveBalance: initialBalance
        );
    }

    [Fact]
    public void DeductLeaveBalance_WhenSufficient_ShouldDecreaseBalance()
    {
        // Arrange
        var employee = CreateSampleEmployee(initialBalance: 15);

        // Act
        employee.DeductLeaveBalance(5);

        // Assert
        employee.AnnualLeaveBalance.Should().Be(10);
    }

    [Fact]
    public void DeductLeaveBalance_WhenInsufficient_ShouldThrowInsufficientLeaveBalanceException()
    {
        // Arrange
        var employee = CreateSampleEmployee(initialBalance: 3);

        // Act
        var act = () => employee.DeductLeaveBalance(5);

        // Assert
        act.Should().Throw<InsufficientLeaveBalanceException>()
            .WithMessage("*Requested: 5 days, Available: 3 days*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void DeductLeaveBalance_WhenZeroOrNegative_ShouldThrowInvalidLeaveOperationException(int invalidDays)
    {
        // Arrange
        var employee = CreateSampleEmployee(initialBalance: 15);

        // Act
        var act = () => employee.DeductLeaveBalance(invalidDays);

        // Assert
        act.Should().Throw<InvalidLeaveOperationException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void AddLeaveBalance_WhenPositive_ShouldIncreaseBalance()
    {
        // Arrange
        var employee = CreateSampleEmployee(initialBalance: 10);

        // Act
        employee.AddLeaveBalance(5);

        // Assert
        employee.AnnualLeaveBalance.Should().Be(15);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRoleProperty()
    {
        // Arrange
        var employee = CreateSampleEmployee();

        // Act
        employee.ChangeRole(UserRole.HRManager);

        // Assert
        employee.Role.Should().Be(UserRole.HRManager);
    }
}
