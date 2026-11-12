using HumanResources.Application.Common.Interfaces;
using HumanResources.Application.DTOs;
using HumanResources.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HumanResources.Application.Services;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
}

public class DepartmentService : IDepartmentService
{
    private readonly IApplicationDbContext _context;

    public DepartmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .Include(d => d.Employees)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(d => new DepartmentDto(
            d.Id,
            d.Name,
            d.Description,
            d.Employees.Count
        )).ToList();
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = new Department(request.Name, request.Description);
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(department.Id, department.Name, department.Description, 0);
    }
}
