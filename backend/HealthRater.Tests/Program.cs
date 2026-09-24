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

Console.WriteLine("-- Auth (password hashing / validation / user store) --");
all.AddRange(AuthTests.All());

var failed = TestRunner.Run(all);
Environment.Exit(failed == 0 ? 0 : 1);
