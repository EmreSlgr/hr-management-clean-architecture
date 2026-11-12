using HumanResources.Application.Common.Interfaces;
using HumanResources.Application.DTOs;
using HumanResources.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanResources.Application.Services;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
}

public class EmployeeService : IEmployeeService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .OrderBy(e => e.FirstName)
            .ToListAsync(cancellationToken);

        return employees.Select(MapToDto).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {id} not found.");

        return MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();
        var exists = await _context.Employees.AnyAsync(e => e.Email == email, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"An employee with email '{request.Email}' already exists.");

        var department = await _context.Departments.FindAsync(new object[] { request.DepartmentId }, cancellationToken);
        if (department == null)
            throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var employee = new Employee(
            request.FirstName,
            request.LastName,
            request.Email,
            passwordHash,
            request.JobTitle,
            request.Role,
            request.DepartmentId,
            DateTime.UtcNow,
            request.AnnualLeaveBalance
        );

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(employee);
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.FullName,
            employee.Email,
            employee.JobTitle,
            employee.Role.ToString(),
            employee.DepartmentId,
            employee.Department?.Name ?? string.Empty,
            employee.HireDate,
            employee.AnnualLeaveBalance
        );
    }
}
