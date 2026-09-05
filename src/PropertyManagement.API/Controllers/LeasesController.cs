using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Leases;
using PropertyManagement.Application.Services;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeasesController : ControllerBase
{
    private readonly ILeaseService _leaseService;
    private readonly ICurrentUserService _currentUserService;

    public LeasesController(ILeaseService leaseService, ICurrentUserService currentUserService)
    {
        _leaseService = leaseService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get leases. Tenants only see their own leases; Managers/Admins can see all and filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LeaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tenantId, [FromQuery] Guid? unitId, [FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var isTenantOnly = !_currentUserService.Roles.Contains("Admin") && !_currentUserService.Roles.Contains("PropertyManager");
        var effectiveTenantId = isTenantOnly ? _currentUserService.UserId : tenantId;

        var result = await _leaseService.GetLeasesAsync(effectiveTenantId, unitId, activeOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get lease details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _leaseService.GetLeaseByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        var isTenantOnly = !_currentUserService.Roles.Contains("Admin") && !_currentUserService.Roles.Contains("PropertyManager");
        if (isTenantOnly && result.Data?.TenantId != _currentUserService.UserId)
        {
            return Forbid();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new lease with automatic overlap checking (Admin or Property Manager only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLeaseDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _leaseService.CreateLeaseAsync(dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update lease terms (Admin or Property Manager only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaseDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _leaseService.UpdateLeaseAsync(id, dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Terminate an active lease (Admin or Property Manager only)
    /// </summary>
    [HttpPost("{id:guid}/terminate")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Terminate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _leaseService.TerminateLeaseAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a lease permanently (Admin or Property Manager only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _leaseService.DeleteLeaseAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.Message.Contains("portfolio", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}
