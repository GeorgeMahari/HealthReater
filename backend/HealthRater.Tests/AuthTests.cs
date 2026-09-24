using HealthRater.Core.Auth;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class AuthTests
{
    public static List<(string, Action)> All() => new()
    {
        ("Auth: hashed password verifies with the correct password", () =>
        {
            var hash = PasswordHasher.Hash("Secret123");
            Assert.True(PasswordHasher.Verify("Secret123", hash), "Correct password verifies");
        }),

        ("Auth: hashed password rejects a wrong password", () =>
        {
            var hash = PasswordHasher.Hash("Secret123");
            Assert.False(PasswordHasher.Verify("secret123", hash), "Wrong password rejected");
        }),

        ("Auth: same password hashes differently (random salt) and never stores plaintext", () =>
        {
            var a = PasswordHasher.Hash("Secret123");
            var b = PasswordHasher.Hash("Secret123");
            Assert.False(a == b, "Two hashes of the same password differ");
            Assert.False(a.Contains("Secret123"), "Hash does not contain the plaintext");
        }),

        ("Auth: malformed stored hash is rejected, not thrown", () =>
        {
            Assert.False(PasswordHasher.Verify("Secret123", "not-a-hash"), "Garbage format");
            Assert.False(PasswordHasher.Verify("Secret123", "PBKDF2-SHA512$abc$%%%$%%%"), "Bad fields");
        }),

        ("Auth: valid registration passes validation", () =>
        {
            var outcome = AuthValidator.ValidateRegistration("Ana", "ana@example.com", "Secret123");
            Assert.True(outcome.IsValid, "Valid registration");
        }),

        ("Auth: registration rejects empty name, bad email and weak password", () =>
        {
            Assert.False(AuthValidator.ValidateRegistration(" ", "ana@example.com", "Secret123").IsValid, "Empty name");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "ana@", "Secret123").IsValid, "Bad email");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Ana <ana@example.com>", "Secret123").IsValid, "Display-name email");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "ana@example.com", "short1").IsValid, "Too short");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "ana@example.com", "onlyletters").IsValid, "No digit");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "ana@example.com", "12345678").IsValid, "No letter");
        }),

        ("Auth: user store creates, finds (case-insensitive email) and blocks duplicates", () =>
        {
            WithTempStore(path =>
            {
                var store = new JsonFileUserStore(path);
                var created = store.CreateAsync(" Ana ", "Ana@Example.com", "hash").GetAwaiter().GetResult();
                Assert.True(created is not null, "User created");
                Assert.Equal("ana@example.com", created!.Email, "Email normalized");
                Assert.Equal("Ana", created.Name, "Name trimmed");

                var found = store.FindByEmailAsync("ANA@example.COM ").GetAwaiter().GetResult();
                Assert.Equal(created.Id, found?.Id, "Found by email regardless of case");

                var duplicate = store.CreateAsync("Other", "ana@example.com", "hash2").GetAwaiter().GetResult();
                Assert.True(duplicate is null, "Duplicate email rejected");
            });
        }),

        ("Auth: user store persists users to disk", () =>
        {
            WithTempStore(path =>
            {
                var created = new JsonFileUserStore(path).CreateAsync("Ana", "ana@example.com", "hash").GetAwaiter().GetResult();
                var reloaded = new JsonFileUserStore(path).FindByIdAsync(created!.Id).GetAwaiter().GetResult();
                Assert.Equal("ana@example.com", reloaded?.Email, "User reloaded from file");
            });
        }),
    };

    private static void WithTempStore(Action<string> test)
    {
        var dir = Path.Combine(Path.GetTempPath(), "healthrater-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            test(Path.Combine(dir, "users.json"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
