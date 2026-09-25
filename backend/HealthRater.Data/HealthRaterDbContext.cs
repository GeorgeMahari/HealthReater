using HealthRater.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthRater.Data;

/// <summary>
/// Provider-agnostic model. Each supported provider has a thin subclass with its own
/// migrations folder (EF Core migrations are provider-specific):
/// <see cref="SqliteHealthRaterDbContext"/> (development default) and
/// <see cref="SqlServerHealthRaterDbContext"/> (production / optional local SQL Server).
/// Application code depends only on this base type.
/// </summary>
public abstract class HealthRaterDbContext : DbContext
{
    protected HealthRaterDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserAvatar> UserAvatars => Set<UserAvatar>();
    public DbSet<HealthAssessment> HealthAssessments => Set<HealthAssessment>();
    public DbSet<AssessmentParameterScore> AssessmentParameterScores => Set<AssessmentParameterScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.Property(u => u.Email).HasMaxLength(254).IsRequired();
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            user.Property(u => u.FirstName).HasMaxLength(80).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(80).IsRequired();
            user.Ignore(u => u.DisplayName);

            user.HasOne(u => u.Avatar)
                .WithOne(a => a.User)
                .HasForeignKey<UserAvatar>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            user.HasMany(u => u.RefreshTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            user.HasMany(u => u.HealthAssessments)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAvatar>(avatar =>
        {
            avatar.HasKey(a => a.UserId);
            avatar.Property(a => a.ContentType).HasMaxLength(20).IsRequired();
            avatar.Property(a => a.Data).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<HealthAssessment>(assessment =>
        {
            assessment.HasKey(a => a.Id);
            assessment.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            assessment.Property(a => a.ScoringVersion).HasMaxLength(40).IsRequired();
            assessment.Property(a => a.Sex).HasMaxLength(20).IsRequired();
            assessment.Property(a => a.HeightUnit).HasMaxLength(10).IsRequired();
            assessment.Property(a => a.WeightUnit).HasMaxLength(10).IsRequired();
            assessment.Property(a => a.WaistUnit).HasMaxLength(10).IsRequired();
            assessment.Property(a => a.HipUnit).HasMaxLength(10).IsRequired();
            assessment.Property(a => a.InputSnapshotJson).IsRequired();

            MapState(assessment, a => a.EnergyStrengthStamina, "EnergyStrengthStamina");
            MapState(assessment, a => a.MentalEmotional, "MentalEmotional");
            MapState(assessment, a => a.Immunity, "Immunity");
            MapState(assessment, a => a.Longevity, "Longevity");

            // History/calendar queries: "this user's completed scans, by date".
            assessment.HasIndex(a => a.UserId);
            assessment.HasIndex(a => a.CompletedAt);
            assessment.HasIndex(a => new { a.UserId, a.Status, a.CompletedAt });

            assessment.HasMany(a => a.ParameterScores)
                .WithOne(p => p.Assessment)
                .HasForeignKey(p => p.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            assessment.ToTable(t =>
            {
                t.HasCheckConstraint("CK_HealthAssessments_Total", "TotalHealthRating >= 0 AND TotalHealthRating <= TotalPossibleScore");
                t.HasCheckConstraint("CK_HealthAssessments_Age", "AgeAtAssessment BETWEEN 18 AND 100");
            });
        });

        modelBuilder.Entity<AssessmentParameterScore>(parameter =>
        {
            parameter.HasKey(p => p.Id);
            parameter.Property(p => p.ParameterKey).HasMaxLength(64).IsRequired();
            parameter.Property(p => p.ParameterName).HasMaxLength(100).IsRequired();
            parameter.Property(p => p.RawText).HasMaxLength(200);
            parameter.Property(p => p.Unit).HasMaxLength(40);
            parameter.HasIndex(p => p.AssessmentId);
            parameter.HasIndex(p => new { p.AssessmentId, p.ParameterKey }).IsUnique();
            parameter.ToTable(t => t.HasCheckConstraint("CK_AssessmentParameterScores_Score", "Score BETWEEN 1 AND 10"));
        });

        // All timestamps are written as UTC; make sure they are read back flagged as UTC
        // (SQLite has no timezone-aware type) so the API serializes them with a "Z".
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTime)) property.SetValueConverter(utc);
                else if (property.ClrType == typeof(DateTime?)) property.SetValueConverter(utcNullable);
            }
        }
    }

    private static void MapState(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<HealthAssessment> assessment,
        System.Linq.Expressions.Expression<Func<HealthAssessment, StateScoreSnapshot?>> navigation,
        string prefix)
    {
        assessment.OwnsOne(navigation, state =>
        {
            state.Property(s => s.NormalizedScore).HasColumnName($"{prefix}Score");
            state.Property(s => s.RawScore).HasColumnName($"{prefix}Raw");
            state.Property(s => s.MaxRawScore).HasColumnName($"{prefix}Max");
        });
        assessment.Navigation(navigation!).IsRequired();
    }
}

public class SqliteHealthRaterDbContext : HealthRaterDbContext
{
    public SqliteHealthRaterDbContext(DbContextOptions<SqliteHealthRaterDbContext> options) : base(options)
    {
    }
}

public class SqlServerHealthRaterDbContext : HealthRaterDbContext
{
    public SqlServerHealthRaterDbContext(DbContextOptions<SqlServerHealthRaterDbContext> options) : base(options)
    {
    }
}
