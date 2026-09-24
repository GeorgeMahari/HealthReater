namespace HealthRater.Tests.Framework;

/// <summary>
/// Minimal dependency-free test harness. xUnit/NUnit require NuGet packages which
/// cannot be restored in this offline sandbox (api.nuget.org is not reachable), so
/// tests here are plain static methods collected and run by hand from Program.cs,
/// with pass/fail counted and printed. Swap this out for real xUnit once NuGet
/// access is available — the assertions themselves (Assert.*) mirror xUnit's API
/// closely enough to make that a low-effort migration.
/// </summary>
public static class Assert
{
    public static void Equal<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{context}: expected {expected}, got {actual}");
        }
    }

    public static void InRange(double value, double min, double max, string context)
    {
        if (value < min || value > max)
        {
            throw new Exception($"{context}: expected value in [{min},{max}], got {value}");
        }
    }

    public static void True(bool condition, string context)
    {
        if (!condition) throw new Exception($"{context}: expected true, got false");
    }

    public static void False(bool condition, string context)
    {
        if (condition) throw new Exception($"{context}: expected false, got true");
    }
}

public static class TestRunner
{
    public static int Run(List<(string Name, Action Test)> tests)
    {
        int passed = 0, failed = 0;
        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"  [PASS] {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name} -> {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {passed + failed}, Passed: {passed}, Failed: {failed}");
        return failed;
    }
}
