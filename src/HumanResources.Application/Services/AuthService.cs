using HumanResources.Application.Common.Interfaces;
using HumanResources.Application.DTOs;
using HumanResources.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HumanResources.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtGenerator,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _jwtGenerator = jwtGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLowerInvariant().Trim();
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);

        if (employee == null || !_passwordHasher.VerifyPassword(request.Password, employee.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var token = _jwtGenerator.GenerateToken(employee);

        return new AuthResponse(
            employee.Id,
            employee.FullName,
            employee.Email,
            employee.Role.ToString(),
            token,
            employee.AnnualLeaveBalance
        );
    }
}
