using System.Security.Cryptography;

namespace HealthRater.Core.Auth;

/// <summary>
/// PBKDF2-HMAC-SHA512 password hashing (OWASP-recommended 210,000 iterations) with a
/// random 16-byte salt per password. Stored format:
/// <c>PBKDF2-SHA512$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;</c>
/// so the iteration count can be raised later without invalidating existing users.
/// </summary>
public static class PasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA512";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
