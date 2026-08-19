using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Application.Modules.Trainee;
using ERP.Application.Modules.Trainee.TraineeInput;
using ERP.Application.Modules.Trainee.TraineeOutput;
using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/[controller]")]
[Tags("Trainees")]
//[Authorize(Roles = Roles.Administrator)]
public class TraineesController : ApiControllerBase
{
    private readonly ITraineeService _traineeService;

    public TraineesController(
        ITraineeService traineeService)
    {
        _traineeService = traineeService;
    }

    /// <summary>
    /// Gets a trainee or parent by phone number.
    /// </summary>
    /// <remarks>
    /// Searches the system using the phone number.
    /// If the phone belongs to a parent, the response also includes
    /// the parent's children.
    /// </remarks>
    [HttpGet("by-phone")]
    [EndpointName("GetTraineeByPhone")]
    [EndpointSummary("Get trainee by phone number")]
    [EndpointDescription(
        "Searches for a trainee or parent using the phone number.")]
    [ProducesResponseType(
        typeof(TraineeLookupResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPhone(
        [FromQuery] string phoneNumber,
        CancellationToken cancellationToken)
    {
        var result = await _traineeService.GetByPhoneAsync(
            phoneNumber,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Adds existing trainees to an academy.
    /// </summary>
    /// <remarks>
    /// Adds previously registered trainees to the specified academy.
    /// The trainees themselves are not created or modified.
    /// </remarks>
    [HttpPost("existing")]
    [EndpointName("AddExistingTraineesToAcademy")]
    [EndpointSummary("Add existing trainees to academy")]
    [EndpointDescription(
        "Adds existing trainees to an academy.")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddExisting(
        [FromBody] AddExistingTraineesRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _traineeService.AddExistingTraineesToAcademyAsync(
                request,
                cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Creates a new trainee registration.
    /// </summary>
    /// <remarks>
    /// Creates a new trainee or parent with children and
    /// automatically registers the trainees in the academy.
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateTrainee")]
    [EndpointSummary("Create trainee")]
    [EndpointDescription(
        "Creates a new trainee registration and adds it to an academy.")]
    [ProducesResponseType(
        typeof(TraineeResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromForm] CreateTraineeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _traineeService.CreateTraineeAsync(
            request,
            cancellationToken);

        return ToCreatedResult(
            result,
            nameof(GetByPhone),
            value => new
            {
                phoneNumber = value.PhoneNumber
            });
    }

    [HttpGet("academy/{academyId:guid}")]
    [EndpointName("GetAcademyTrainees")]
    [EndpointSummary("Get academy trainees")]
    [EndpointDescription(
    "Gets trainees registered in an academy with an optional status filter.")]
    [ProducesResponseType(
    typeof(PagedList<AcademyTraineeDto>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAcademyTrainees(
    Guid academyId,
    [FromQuery] TraineeStatus? status,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
    {
        var result = await _traineeService.GetAcademyTraineesAsync(
            academyId,
            status,
            page,
            pageSize,
            cancellationToken);

        return ToActionResult(result);
    }
}