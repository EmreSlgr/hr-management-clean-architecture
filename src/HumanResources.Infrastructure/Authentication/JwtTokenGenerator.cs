using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HumanResources.Application.Common.Interfaces;
using HumanResources.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HumanResources.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Employee employee)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForCleanArchitectureHRSystem_2026_Minimum32Chars!";
        var issuer = _configuration["Jwt:Issuer"] ?? "HumanResourcesApi";
        var audience = _configuration["Jwt:Audience"] ?? "HumanResourcesClients";
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 120;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Email, employee.Email),
            new(ClaimTypes.Name, employee.FullName),
            new(ClaimTypes.Role, employee.Role.ToString()),
            new("departmentId", employee.DepartmentId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(securityToken);
    }
}
