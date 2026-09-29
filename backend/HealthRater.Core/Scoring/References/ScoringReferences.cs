using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HealthRater.Core.Models;

namespace HealthRater.Core.Scoring.References;

/// <summary>How a reference turns a measured value into a 1–10 score.</summary>
public enum ReferenceMethod
{
    /// <summary>10 at <c>Center</c>, minus <c>PenaltyPerUnit</c> per unit away from it (either direction).</summary>
    IdealCenter,

    /// <summary>10 up to <c>IdealMax</c>, minus <c>PenaltyPerUnit</c> per unit above it.</summary>
    UpperThreshold,

    /// <summary><c>value / Norm × 10</c> — e.g. repetitions relative to an "excellent" norm.</summary>
    RatioToNorm,

    /// <summary>Explicit table: the first band with <c>Min ≤ value &lt; Max</c> gives the score.</summary>
    Bands,
}

/// <summary>Where a reference's numbers come from.</summary>
public enum ReferenceStatus
{
    /// <summary>A placeholder heuristic — not an official HealthRater rule. Replace when official tables exist.</summary>
    Provisional,

    /// <summary>An official HealthRater rule.</summary>
    Official,
}

public class ScoreBand
{
    /// <summary>Inclusive lower bound; null = no lower bound.</summary>
    public double? Min { get; set; }

    /// <summary>Exclusive upper bound; null = no upper bound.</summary>
    public double? Max { get; set; }

    public int Score { get; set; }
}

/// <summary>
/// One configurable scoring reference: for <see cref="ParameterKey"/>, people of
/// <see cref="Sex"/> (null = any sex) aged <see cref="MinAge"/>–<see cref="MaxAge"/> (inclusive)
/// are scored with <see cref="Method"/> and its parameters.
/// </summary>
public class ScoringReference
{
    public string ParameterKey { get; set; } = "";
    public Sex? Sex { get; set; }
    public int MinAge { get; set; } = 18;
    public int MaxAge { get; set; } = 100;
    public ReferenceMethod Method { get; set; }
    public ReferenceStatus Status { get; set; } = ReferenceStatus.Provisional;
    public string? Note { get; set; }

    public double? Center { get; set; }
    public double? IdealMax { get; set; }
    public double? PenaltyPerUnit { get; set; }
    public double? Norm { get; set; }
    public List<ScoreBand>? Bands { get; set; }

    public int Score(double value) => Method switch
    {
        ReferenceMethod.IdealCenter => ScoringConfig.Clamp1To10(10 - Math.Abs(value - Center!.Value) * PenaltyPerUnit!.Value),
        ReferenceMethod.UpperThreshold => ScoringConfig.Clamp1To10(10 - Math.Max(0, value - IdealMax!.Value) * PenaltyPerUnit!.Value),
        ReferenceMethod.RatioToNorm => Norm!.Value <= 0 ? 1 : ScoringConfig.Clamp1To10(value / Norm.Value * 10),
        ReferenceMethod.Bands => Math.Clamp(
            (Bands!.FirstOrDefault(b => (b.Min is null || value >= b.Min) && (b.Max is null || value < b.Max))
                ?? throw new InvalidOperationException($"No band of the '{ParameterKey}' reference covers the value {value}.")).Score,
            1, 10),
        _ => throw new InvalidOperationException($"Unknown reference method {Method}."),
    };

    internal string Describe() =>
        $"{ParameterKey} / {(Sex?.ToString() ?? "any sex")} / ages {MinAge}–{MaxAge}";
}

/// <summary>
/// The set of sex- and age-specific scoring references, loaded from
/// <c>scoring-references.json</c> (embedded in this assembly; an alternative file can be
/// supplied with <see cref="LoadFromFile"/>). Selection for (parameter, sex, age): a
/// reference for that exact sex wins over a sex-neutral one; the age must fall in its range.
/// The catalog is validated when loaded so that every parameter it covers has exactly one
/// matching reference for every sex and every age from 18 to 100.
/// </summary>
public class ScoringReferenceCatalog
{
    public const int MinSupportedAge = 18;
    public const int MaxSupportedAge = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static ScoringReferenceCatalog? _current;

    public string Version { get; }
    public IReadOnlyList<ScoringReference> References { get; }

    public ScoringReferenceCatalog(string version, IEnumerable<ScoringReference> references)
    {
        Version = version;
        References = references.ToList();
        Validate();
    }

    /// <summary>The catalog used by the scoring engine. Defaults to the embedded file.</summary>
    public static ScoringReferenceCatalog Current
    {
        get => _current ??= LoadEmbedded();
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static ScoringReferenceCatalog LoadEmbedded()
    {
        var assembly = typeof(ScoringReferenceCatalog).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("scoring-references.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        return Parse(stream);
    }

    public static ScoringReferenceCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Parse(stream);
    }

    public static ScoringReferenceCatalog Parse(Stream json)
    {
        var file = JsonSerializer.Deserialize<CatalogFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("The scoring reference file is empty.");
        return new ScoringReferenceCatalog(file.Version ?? "unversioned", file.References ?? new());
    }

    public bool Covers(string parameterKey) => References.Any(r => r.ParameterKey == parameterKey);

    /// <summary>The reference that applies to this parameter for this person.</summary>
    public ScoringReference Find(string parameterKey, Sex sex, int age)
    {
        var matches = References
            .Where(r => r.ParameterKey == parameterKey && (r.Sex is null || r.Sex == sex) && age >= r.MinAge && age <= r.MaxAge)
            .ToList();
        var specific = matches.Where(r => r.Sex == sex).ToList();
        var chosen = specific.Count > 0 ? specific : matches;
        return chosen.Count == 1
            ? chosen[0]
            : throw new InvalidOperationException(
                $"Expected exactly one scoring reference for {parameterKey} / {sex} / age {age}, found {chosen.Count}.");
    }

    /// <summary>calculateParameterScore(parameter, value, sex, age) for reference-driven parameters.</summary>
    public int Score(string parameterKey, double value, ScoringContext context) =>
        Find(parameterKey, context.Sex, context.Age).Score(value);

    private void Validate()
    {
        var errors = new List<string>();
        foreach (var r in References)
        {
            if (string.IsNullOrWhiteSpace(r.ParameterKey)) errors.Add("A reference is missing its parameterKey.");
            if (r.MinAge < MinSupportedAge || r.MaxAge > MaxSupportedAge || r.MinAge > r.MaxAge)
                errors.Add($"{r.Describe()}: age range must lie within {MinSupportedAge}–{MaxSupportedAge}.");
            var missing = r.Method switch
            {
                ReferenceMethod.IdealCenter when r.Center is null || r.PenaltyPerUnit is null => "center and penaltyPerUnit",
                ReferenceMethod.UpperThreshold when r.IdealMax is null || r.PenaltyPerUnit is null => "idealMax and penaltyPerUnit",
                ReferenceMethod.RatioToNorm when r.Norm is null => "norm",
                ReferenceMethod.Bands when r.Bands is not { Count: > 0 } => "bands",
                _ => null,
            };
            if (missing is not null) errors.Add($"{r.Describe()}: method {r.Method} needs {missing}.");
            if (r.Bands?.Any(b => b.Score is < 1 or > 10) == true) errors.Add($"{r.Describe()}: band scores must be 1–10.");
        }

        // Every covered parameter must resolve to exactly one reference for each sex and age.
        foreach (var key in References.Select(r => r.ParameterKey).Distinct())
        {
            foreach (var sex in Enum.GetValues<Sex>())
            {
                for (var age = MinSupportedAge; age <= MaxSupportedAge; age++)
                {
                    try
                    {
                        Find(key, sex, age);
                    }
                    catch (InvalidOperationException ex)
                    {
                        errors.Add(ex.Message);
                        break; // one message per parameter/sex is enough
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid scoring reference configuration:\n - " + string.Join("\n - ", errors));
        }
    }

    private sealed class CatalogFile
    {
        public string? Version { get; set; }
        public List<ScoringReference>? References { get; set; }
    }
}
