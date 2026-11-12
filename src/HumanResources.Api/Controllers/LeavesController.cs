using System.Security.Claims;
using HumanResources.Application.DTOs;
using HumanResources.Application.Services;
using HumanResources.Domain.Enums;
using HumanResources.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanResources.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id)) return id;
        throw new UnauthorizedAccessException("User identity claim not found.");
    }

    /// <summary>
    /// Get leave requests submitted by currently logged-in user
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetMyLeaves()
    {
        var leaves = await _leaveService.GetMyLeavesAsync(GetCurrentUserId());
        return Ok(leaves);
    }

    /// <summary>
    /// Submit a new leave request (Annual, Sick, Maternity, Unpaid)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> SubmitLeave([FromBody] CreateLeaveRequestInput input)
    {
        try
        {
            var created = await _leaveService.SubmitLeaveAsync(GetCurrentUserId(), input);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(int id)
    {
        try
        {
            var leave = await _leaveService.GetByIdAsync(id);
            return Ok(leave);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a pending leave request owned by current user
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(int id)
    {
        try
        {
            var result = await _leaveService.CancelLeaveAsync(id, GetCurrentUserId());
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all company leave requests (HRManager and Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "HRManager,Admin")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetAll([FromQuery] LeaveStatus? status)
    {
        var leaves = await _leaveService.GetAllLeavesAsync(status);
        return Ok(leaves);
    }

    /// <summary>
    /// Approve a pending leave request (HRManager and Admin only)
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "HRManager,Admin")]
    public async Task<ActionResult<LeaveRequestDto>> Approve(int id)
    {
        try
        {
            var result = await _leaveService.ApproveLeaveAsync(id, GetCurrentUserId());
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reject a pending leave request with a mandatory reason (HRManager and Admin only)
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "HRManager,Admin")]
    public async Task<ActionResult<LeaveRequestDto>> Reject(int id, [FromBody] RejectLeaveInput input)
    {
        try
        {
            var result = await _leaveService.RejectLeaveAsync(id, GetCurrentUserId(), input.Reason);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
