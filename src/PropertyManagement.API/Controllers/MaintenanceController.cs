using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Maintenance;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ICurrentUserService _currentUserService;

    public MaintenanceController(IMaintenanceService maintenanceService, ICurrentUserService currentUserService)
    {
        _maintenanceService = maintenanceService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// List maintenance tickets. Tenants see only their own requests; Managers/Admins can see all and filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MaintenanceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? unitId,
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] MaintenancePriority? priority,
        CancellationToken cancellationToken)
    {
        var isTenantOnly = !_currentUserService.Roles.Contains("Admin") && !_currentUserService.Roles.Contains("PropertyManager");
        var effectiveTenantId = isTenantOnly ? _currentUserService.UserId : tenantId;

        var result = await _maintenanceService.GetRequestsAsync(effectiveTenantId, unitId, status, priority, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get maintenance ticket details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _maintenanceService.GetRequestByIdAsync(id, cancellationToken);
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
    /// Submit a new maintenance ticket (Tenants or Property Managers)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tenantId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _maintenanceService.CreateRequestAsync(dto, tenantId, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update maintenance request status, assign technician, or add resolution notes (Admin or Property Manager only)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMaintenanceStatusDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _maintenanceService.UpdateRequestStatusAsync(id, dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a maintenance request (Admin or Property Manager only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _maintenanceService.DeleteRequestAsync(id, cancellationToken);
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
