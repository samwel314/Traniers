using ERP.Application.Common.Models;
using ERP.Application.Modules.Academy;
using ERP.Application.Modules.Academy.AcademyInput;
using ERP.Application.Modules.Academy.AcademyOutput;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/[controller]")]
[Tags("Academies")]
public class AcademiesController : ApiControllerBase
{
    private readonly IAcademyService _academyService;

    public AcademiesController(
        IAcademyService academyService)
    {
        _academyService = academyService;
    }

    /// <summary>
    /// Creates a new academy.
    /// </summary>
    /// <remarks>
    /// Creates an academy with its information and optional logo.
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateAcademy")]
    [EndpointSummary("Create academy")]
    [EndpointDescription("Creates a new academy.")]
    [ProducesResponseType(
        typeof(AcademyResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromForm] CreateAcademyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.CreateAcademyAsync(
            request,
            cancellationToken);

        return ToCreatedResult(
            result,
            nameof(GetById),
            value => new { academyId = value.Id });
    }

    /// <summary>
    /// Gets an academy by ID.
    /// </summary>
    [HttpGet("{academyId:guid}")]
    [EndpointName("GetAcademyById")]
    [EndpointSummary("Get academy by ID")]
    [EndpointDescription("Gets academy details by its unique identifier.")]
    [ProducesResponseType(
        typeof(AcademyDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid academyId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.GetAcademyByIdAsync(
            academyId,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets all active academies with pagination.
    /// </summary>
    [HttpGet]
    [EndpointName("GetAllAcademies")]
    [EndpointSummary("Get all academies")]
    [EndpointDescription("Gets a paginated list of active academies.")]
    [ProducesResponseType(
        typeof(PagedList<AcademyLockupDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _academyService.GetAllAcademiesAsync(
            page,
            pageSize,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Updates an existing academy.
    /// </summary>
    /// <remarks>
    /// Updates academy information and optionally replaces its logo.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [EndpointName("UpdateAcademy")]
    [EndpointSummary("Update academy")]
    [EndpointDescription("Updates an existing academy.")]
    [ProducesResponseType(
        typeof(AcademyResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromForm] UpdateAcademyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.UpdateAcademyAsync(
            id,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Soft deletes an academy.
    /// </summary>
    [HttpDelete("{academyId:guid}")]
    [EndpointName("DeleteAcademy")]
    [EndpointSummary("Delete academy")]
    [EndpointDescription("Soft deletes an academy.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid academyId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.DeleteAcademyAsync(
            academyId,
            cancellationToken);

        return ToActionResult(result);
    }
}