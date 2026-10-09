using System.Security.Claims;
using HealthRater.Api.Dtos;
using HealthRater.Core.Models;
using HealthRater.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRater.Api.Controllers;

/// <summary>
/// The signed-in user's saved assessments. The owner is always the authenticated user
/// (from the session cookie's claims); no endpoint accepts a user id from the client.
/// Requests for someone else's assessment return 404, not 403, so ids can't be probed.
/// </summary>
[ApiController]
[Authorize]
[Route("api/assessments")]
public class AssessmentsController : ControllerBase
{
    private readonly AssessmentService _assessments;

    public AssessmentsController(AssessmentService assessments)
    {
        _assessments = assessments;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(List<AssessmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssessmentSummaryResponse>>> List()
    {
        var items = await _assessments.ListCompletedAsync(CurrentUserId);
        return Ok(items.Select(AssessmentSummaryResponse.From).ToList());
    }

    /// <summary>
    /// Lightweight month view. <paramref name="timeZone"/> is an IANA id (e.g. "Europe/Bucharest")
    /// used to decide which local day — and month — each scan belongs to. Defaults to UTC.
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(List<CalendarEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CalendarEntryResponse>>> Calendar(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? timeZone = null)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            return BadRequest(new { errors = new[] { "Provide a valid year (2000–2100) and month (1–12)." } });
        }

        TimeZoneInfo zone;
        try
        {
            zone = string.IsNullOrWhiteSpace(timeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return BadRequest(new { errors = new[] { "Unknown time zone." } });
        }

        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, zone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localStart.AddMonths(1), zone);

        var items = await _assessments.ListCompletedBetweenAsync(CurrentUserId, fromUtc, toUtc);
        return Ok(items.Select(s => new CalendarEntryResponse(
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s.CompletedAt, zone)).ToString("yyyy-MM-dd"),
            s.Id,
            s.CompletedAt,
            s.TotalHealthRating,
            s.TotalPossibleScore,
            s.EnergyStrengthStamina,
            s.MentalEmotional,
            s.Immunity,
            s.Longevity)).ToList());
    }

    /// <summary>
    /// The body-fat estimate used when the user answers "I don't know" — calculated from the
    /// signed-in user's profile (sex, age, height, weight) with the configured estimator
    /// (currently Deurenberg et al. 1991). Waist and hip are optional, for estimators that use
    /// them. 409 when the profile is incomplete; 422 when no plausible estimate is possible.
    /// </summary>
    [HttpGet("body-fat-estimate")]
    [ProducesResponseType(typeof(BodyFatEstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BodyFatEstimateResponse>> BodyFatEstimate(
        [FromQuery] double? waistCm = null, [FromQuery] double? hipCm = null)
    {
        var (estimate, profileError, estimateError) = await _assessments.EstimateBodyFatAsync(CurrentUserId, waistCm, hipCm);
        if (profileError is not null)
        {
            return Conflict(new { code = "profile_incomplete", errors = new[] { profileError } });
        }
        if (estimate is null)
        {
            return UnprocessableEntity(new { errors = new[] { estimateError } });
        }
        return Ok(new BodyFatEstimateResponse(
            estimate.BodyFatPercent, nameof(BodyFatSource.Estimated), estimate.MethodId, estimate.MethodName,
            HealthRater.Core.Scoring.BodyFat.BodyFatEstimation.Disclaimer));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssessmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentDetailResponse>> Get(Guid id)
    {
        var assessment = await _assessments.GetAsync(CurrentUserId, id);
        return assessment is null ? NotFound() : Ok(AssessmentDetailResponse.From(assessment));
    }

    /// <summary>
    /// Scores and permanently stores a completed assessment for the signed-in user. The body
    /// holds only the questionnaire answers: sex, age, height and weight come from the user's
    /// profile (any sent in the request are ignored). 409 when the profile is incomplete.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AssessmentDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssessmentDetailResponse>> Create([FromBody] AssessmentAnswers answers)
    {
        var outcome = await _assessments.CreateCompletedAsync(CurrentUserId, answers);
        if (outcome.ProfileError is not null)
        {
            return Conflict(new { code = "profile_incomplete", errors = new[] { outcome.ProfileError } });
        }
        if (outcome.ValidationErrors is not null)
        {
            return BadRequest(new { errors = outcome.ValidationErrors });
        }

        var created = outcome.Assessment!;
        return CreatedAtAction(nameof(Get), new { id = created.Id }, AssessmentDetailResponse.From(created));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _assessments.DeleteAsync(CurrentUserId, id) ? NoContent() : NotFound();
}
