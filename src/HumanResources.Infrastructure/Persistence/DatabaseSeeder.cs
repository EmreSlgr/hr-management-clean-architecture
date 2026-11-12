using HumanResources.Application.Common.Interfaces;
using HumanResources.Domain.Entities;
using HumanResources.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanResources.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, IPasswordHasher hasher, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        if (await _context.Departments.AnyAsync())
        {
            return; // Already seeded
        }

        _logger.LogInformation("Seeding initial HR Management database...");

        // 1. Departments
        var deptTech = new Department("Software Engineering", "Core development and platform engineering team.");
        var deptHr = new Department("People & Culture", "Talent management, payroll, and corporate operations.");
        var deptFinance = new Department("Finance & Accounting", "Corporate budget, treasury, and financial auditing.");

        _context.Departments.AddRange(deptTech, deptHr, deptFinance);
        await _context.SaveChangesAsync();

        // 2. Employees (Admin, HR Manager, Regular Employees)
        var defaultPasswordHash = _hasher.HashPassword("Password123!");

        var admin = new Employee(
            "System",
            "Admin",
            "admin@company.com",
            defaultPasswordHash,
            "Chief Technology Officer",
            UserRole.Admin,
            deptTech.Id,
            DateTime.UtcNow.AddYears(-3),
            annualLeaveBalance: 25
        );

        var hrManager = new Employee(
            "Sarah",
            "Connor",
            "sarah.hr@company.com",
            defaultPasswordHash,
            "HR Lead",
            UserRole.HRManager,
            deptHr.Id,
            DateTime.UtcNow.AddYears(-2),
            annualLeaveBalance: 20
        );

        var employee1 = new Employee(
            "Emre",
            "Salgır",
            "emre.salgir@company.com",
            defaultPasswordHash,
            "Senior Software Developer",
            UserRole.Employee,
            deptTech.Id,
            DateTime.UtcNow.AddYears(-1),
            annualLeaveBalance: 15
        );

        var employee2 = new Employee(
            "Michael",
            "Scott",
            "michael.scott@company.com",
            defaultPasswordHash,
            "Operations Specialist",
            UserRole.Employee,
            deptFinance.Id,
            DateTime.UtcNow.AddMonths(-8),
            annualLeaveBalance: 12
        );

        _context.Employees.AddRange(admin, hrManager, employee1, employee2);
        await _context.SaveChangesAsync();

        // 3. Sample Leave Requests
        var sampleLeave1 = LeaveRequest.Create(
            employee1,
            LeaveType.Annual,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(18),
            "Annual family summer vacation."
        );

        var sampleLeave2 = LeaveRequest.Create(
            employee2,
            LeaveType.Sick,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-4),
            "Medical appointment and recovery."
        );
        sampleLeave2.Approve(hrManager);

        _context.LeaveRequests.AddRange(sampleLeave1, sampleLeave2);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeded successfully with initial departments, employees, and leave requests.");
    }
}
