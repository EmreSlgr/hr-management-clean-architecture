using HumanResources.Application.Common.Interfaces;
using HumanResources.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanResources.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Department
        modelBuilder.Entity<Department>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Description).HasMaxLength(250);

            builder.HasMany(d => d.Employees)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Employee
        modelBuilder.Entity<Employee>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(60);
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(60);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(e => e.Email).IsUnique();
            builder.Property(e => e.PasswordHash).IsRequired();
            builder.Property(e => e.JobTitle).HasMaxLength(100);
            builder.Property(e => e.Role).IsRequired();
            builder.Property(e => e.AnnualLeaveBalance).IsRequired();

            builder.HasMany(e => e.LeaveRequests)
                .WithOne(l => l.Employee)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Reason).IsRequired().HasMaxLength(500);
            builder.Property(l => l.RejectionReason).HasMaxLength(500);
            builder.Property(l => l.LeaveType).IsRequired();
            builder.Property(l => l.Status).IsRequired();

            builder.HasOne(l => l.Approver)
                .WithMany()
                .HasForeignKey(l => l.ApproverId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
