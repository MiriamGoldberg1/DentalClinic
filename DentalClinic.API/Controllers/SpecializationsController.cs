using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Controllers;

/// <summary>תחומי התמחות - פתוח לכולם (משמש בטפסים בצד הלקוח).</summary>
[ApiController]
[Route("api/specializations")]
[AllowAnonymous]
public class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationsController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    /// <summary>שליפת כל תחומי ההתמחות.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SpecializationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SpecializationDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _specializationService.GetAllAsync(ct));
    }
}
