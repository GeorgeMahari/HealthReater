using HealthRater.Core.Models;
using HealthRater.Core.Profile;
using HealthRater.Core.Scoring;
using HealthRater.Core.Scoring.BodyFat;
using HealthRater.Core.Validation;
using HealthRater.Data.Entities;
using HealthRater.Data.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace HealthRater.Data.Services;

/// <summary>Lightweight row for history lists and the calendar (no parameter data).</summary>
public record AssessmentSummary(
    Guid Id,
    DateTime CompletedAt,
    int TotalHealthRating,
    int TotalPossibleScore,
    double Percentage,
    double EnergyStrengthStamina,
    double MentalEmotional,
    double Immunity,
    double Longevity);

/// <summary>Outcome of saving an assessment: the stored snapshot, or why it was refused.</summary>
public record AssessmentCreateResult(HealthAssessment? Assessment, string? ProfileError, List<string>? ValidationErrors);

/// <summary>
/// All assessment reads and writes. Every method takes the authenticated user's id —
/// taken from the server-side identity, never from the request — and every query is
/// filtered by it, so one user can never read or delete another user's data.
/// </summary>
public class AssessmentService
{
    private readonly HealthRaterDbContext _db;

    public AssessmentService(HealthRaterDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// The user's profile data for an assessment (sex, age calculated for <paramref name="today"/>
    /// from the date of birth, height, weight), read from the database; or an error when the
    /// profile is incomplete or the age is out of range.
    /// </summary>
    public async Task<(ProfileSnapshot? Profile, DateOnly? DateOfBirth, string? Error)> GetScoringContextAsync(Guid userId, DateOnly today)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Sex, u.DateOfBirth, u.HeightCm, u.WeightKg })
            .FirstOrDefaultAsync();
        if (user is null) return (null, null, "Account not found.");

        var (profile, error) = ProfileRules.ContextFor(user.Sex, user.DateOfBirth, user.HeightCm, user.WeightKg, today);
        return (profile, user.DateOfBirth, error);
    }

    /// <summary>
    /// Preview of the body-fat estimate HealthRater would use if the user doesn't know their
    /// body fat, from their own profile data (and the waist/hip they've entered so far, for
    /// estimators that use them). Same calculation as on submission.
    /// </summary>
    public async Task<(BodyFatEstimate? Estimate, string? ProfileError, string? EstimateError)> EstimateBodyFatAsync(
        Guid userId, double? waistCm, double? hipCm, DateOnly? today = null)
    {
        var (profile, _, error) = await GetScoringContextAsync(userId, today ?? ProfileRules.Today());
        if (profile is null) return (null, error, null);

        var input = AssessmentInput.From(new AssessmentAnswers { WaistCm = waistCm ?? 0, HipCm = hipCm ?? 0 }, profile);
        var (estimate, estimateError) = BodyFatEstimation.TryEstimate(BodyFatEstimation.InputFor(input));
        return (estimate, null, estimateError);
    }

    /// <summary>
    /// Scores the answers with the user's own profile data and stores the result as a new
    /// completed assessment. Sex, age, height and weight always come from the profile, never
    /// from the caller.
    /// <paramref name="today"/> is only overridable for tests.
    /// </summary>
    public async Task<AssessmentCreateResult> CreateCompletedAsync(Guid userId, AssessmentAnswers answers, DateOnly? today = null)
    {
        var (profile, dateOfBirth, error) = await GetScoringContextAsync(userId, today ?? ProfileRules.Today());
        if (profile is null) return new AssessmentCreateResult(null, error, null);

        var input = AssessmentInput.From(answers, profile);
        var validation = AssessmentValidator.Validate(input);
        if (!validation.IsValid) return new AssessmentCreateResult(null, null, validation.Errors);

        var result = HealthRatingEngine.Calculate(input);
        var assessment = AssessmentSnapshotBuilder.Build(userId, input, result, DateTime.UtcNow, dateOfBirth);
        _db.HealthAssessments.Add(assessment);
        await _db.SaveChangesAsync();
        return new AssessmentCreateResult(assessment, null, null);
    }

    /// <summary>The user's completed assessments, newest first. Drafts are excluded.</summary>
    public Task<List<AssessmentSummary>> ListCompletedAsync(Guid userId) =>
        CompletedFor(userId)
            .OrderByDescending(a => a.CompletedAt)
            .Select(ToSummary)
            .ToListAsync();

    /// <summary>Completed assessments whose completion time falls in [fromUtc, toUtc).</summary>
    public Task<List<AssessmentSummary>> ListCompletedBetweenAsync(Guid userId, DateTime fromUtc, DateTime toUtc) =>
        CompletedFor(userId)
            .Where(a => a.CompletedAt >= fromUtc && a.CompletedAt < toUtc)
            .OrderBy(a => a.CompletedAt)
            .Select(ToSummary)
            .ToListAsync();

    /// <summary>Full snapshot including every stored parameter (41, or 39 for v1 assessments), or null if it doesn't exist or isn't this user's.</summary>
    public Task<HealthAssessment?> GetAsync(Guid userId, Guid assessmentId) =>
        _db.HealthAssessments
            .AsNoTracking()
            .Include(a => a.ParameterScores.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.UserId == userId);

    /// <summary>Deletes the assessment only if it belongs to the user. Returns false otherwise.</summary>
    public async Task<bool> DeleteAsync(Guid userId, Guid assessmentId)
    {
        var assessment = await _db.HealthAssessments
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.UserId == userId);
        if (assessment is null) return false;

        // Parameter scores are removed by the database's cascading foreign key.
        _db.HealthAssessments.Remove(assessment);
        await _db.SaveChangesAsync();
        return true;
    }

    private IQueryable<HealthAssessment> CompletedFor(Guid userId) =>
        _db.HealthAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.Status == AssessmentStatus.Completed && a.CompletedAt != null);

    private static readonly System.Linq.Expressions.Expression<Func<HealthAssessment, AssessmentSummary>> ToSummary =
        a => new AssessmentSummary(
            a.Id,
            a.CompletedAt!.Value,
            a.TotalHealthRating,
            a.TotalPossibleScore,
            a.Percentage,
            a.EnergyStrengthStamina.NormalizedScore,
            a.MentalEmotional.NormalizedScore,
            a.Immunity.NormalizedScore,
            a.Longevity.NormalizedScore);
}
