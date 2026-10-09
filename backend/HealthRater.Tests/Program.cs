using HealthRater.Tests;
using HealthRater.Tests.Framework;

Console.WriteLine("HealthRater.Core — Test Suite (dependency-free runner)");
Console.WriteLine("=======================================================");

var all = new List<(string, Action)>();

Console.WriteLine("\n-- Derived Metrics (BMI / WHtR / WHR / unit conversions) --");
all.AddRange(DerivedMetricsTests.All());

Console.WriteLine("-- Scorers (Blood Pressure / Cooper / Functional Power / HR) --");
all.AddRange(ScorerTests.All());

Console.WriteLine("-- Engine (Total Health Rating / Four States) --");
all.AddRange(EngineTests.All());

Console.WriteLine("-- Validation --");
all.AddRange(ValidationTests.All());

Console.WriteLine("-- Auth (password hashing / validation / users / sessions) --");
all.AddRange(AuthTests.All());

Console.WriteLine("-- Persistence (assessments / history / data isolation) --");
all.AddRange(PersistenceTests.All());

Console.WriteLine("-- Profile (edit / avatar / password / account deletion) --");
all.AddRange(ProfileTests.All());

Console.WriteLine("-- Profile context & sex/age-aware scoring --");
all.AddRange(ProfileContextTests.All());

Console.WriteLine("-- Parameter set v2 (substances / body-fat estimation / HRR / legacy 39) --");
all.AddRange(ParameterSetV2Tests.All());

var failed = TestRunner.Run(all);
Environment.Exit(failed == 0 ? 0 : 1);
