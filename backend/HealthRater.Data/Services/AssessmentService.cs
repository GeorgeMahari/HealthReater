using HealthRater.Core.Models;
using HealthRater.Core.Scoring;
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

    /// <summary>Scores the (already validated) input and stores it as a new completed assessment.</summary>
    public async Task<HealthAssessment> CreateCompletedAsync(Guid userId, AssessmentInput input)
    {
        var result = HealthRatingEngine.Calculate(input);
        var assessment = AssessmentSnapshotBuilder.Build(userId, input, result, DateTime.UtcNow);
        _db.HealthAssessments.Add(assessment);
        await _db.SaveChangesAsync();
        return assessment;
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

    /// <summary>Full snapshot including all 39 parameters, or null if it doesn't exist or isn't this user's.</summary>
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
