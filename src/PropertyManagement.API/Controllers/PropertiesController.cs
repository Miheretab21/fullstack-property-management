using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Properties;
using PropertyManagement.Application.Services;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ICurrentUserService _currentUserService;

    public PropertiesController(IPropertyService propertyService, ICurrentUserService currentUserService)
    {
        _propertyService = propertyService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// List all properties with unit occupancy counts
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetAllPropertiesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get property details by ID, including its rentable units
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetPropertyByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new property (Admin or Property Manager only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePropertyDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _propertyService.CreatePropertyAsync(dto, currentUserId, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update property details (Admin or Property Manager only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _propertyService.UpdatePropertyAsync(id, dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a property (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.DeletePropertyAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
