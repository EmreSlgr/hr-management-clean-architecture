using HumanResources.Application.DTOs;
using HumanResources.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanResources.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// List all corporate departments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll()
    {
        var depts = await _departmentService.GetAllAsync();
        return Ok(depts);
    }

    /// <summary>
    /// Create a new department (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentRequest request)
    {
        var dept = await _departmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, dept);
    }
}
