using HumanResources.Application.DTOs;
using HumanResources.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanResources.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// List all active employees
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Get employee details by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        try
        {
            var emp = await _employeeService.GetByIdAsync(id);
            return Ok(emp);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Register a new employee (HRManager and Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HRManager,Admin")]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var emp = await _employeeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
