using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
using HealthRater.Core.Validation;
using Microsoft.AspNetCore.Mvc;

namespace HealthRater.Api.Controllers;

[ApiController]
[Route("api/health-rating")]
public class HealthRatingController : ControllerBase
{
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(HealthRatingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<HealthRatingResult> Calculate([FromBody] AssessmentInput input)
    {
        if (input is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var validation = AssessmentValidator.Validate(input);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors });
        }

        var result = HealthRatingEngine.Calculate(input);
        return Ok(result);
    }
}
