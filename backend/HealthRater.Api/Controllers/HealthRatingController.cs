using System.Security.Claims;
using HealthRater.Core.Models;
using HealthRater.Core.Profile;
using HealthRater.Core.Scoring;
using HealthRater.Core.Validation;
using HealthRater.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRater.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/health-rating")]
public class HealthRatingController : ControllerBase
{
    private readonly AssessmentService _assessments;

    public HealthRatingController(AssessmentService assessments)
    {
        _assessments = assessments;
    }

    /// <summary>
    /// Scores questionnaire answers WITHOUT saving them (a preview). Requires a signed-in user
    /// with a complete profile: sex, age, height and weight come from the profile, never from the request.
    /// Use POST /api/assessments to score and save.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(HealthRatingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HealthRatingResult>> Calculate([FromBody] AssessmentAnswers answers)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (profile, _, error) = await _assessments.GetScoringContextAsync(userId, ProfileRules.Today());
        if (profile is null)
        {
            return Conflict(new { code = "profile_incomplete", errors = new[] { error } });
        }

        var input = AssessmentInput.From(answers, profile);
        var validation = AssessmentValidator.Validate(input);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors });
        }

        return Ok(HealthRatingEngine.Calculate(input));
    }
}
