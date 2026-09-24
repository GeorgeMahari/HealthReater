using HealthRater.Core.Auth;
using HealthRater.Data.Services;
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
            var outcome = AuthValidator.ValidateRegistration("Ana", "Popescu", "ana@example.com", "Secret123");
            Assert.True(outcome.IsValid, "Valid registration");
        }),

        ("Auth: registration rejects empty names, bad email and weak password", () =>
        {
            Assert.False(AuthValidator.ValidateRegistration(" ", "Popescu", "ana@example.com", "Secret123").IsValid, "Empty first name");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "", "ana@example.com", "Secret123").IsValid, "Empty last name");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Popescu", "ana@", "Secret123").IsValid, "Bad email");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Popescu", "Ana <ana@example.com>", "Secret123").IsValid, "Display-name email");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Popescu", "ana@example.com", "short1").IsValid, "Too short");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Popescu", "ana@example.com", "onlyletters").IsValid, "No digit");
            Assert.False(AuthValidator.ValidateRegistration("Ana", "Popescu", "ana@example.com", "12345678").IsValid, "No letter");
        }),

        ("Auth: users are stored with a normalized, unique email", () =>
        {
            using var db = TestDatabase.Create();
            var users = new UserService(db.Context);
            var created = users.CreateAsync(" Ana ", "Popescu", "Ana@Example.com", "hash").GetAwaiter().GetResult();
            Assert.True(created is not null, "User created");
            Assert.Equal("ana@example.com", created!.Email, "Email normalized");
            Assert.Equal("Ana", created.FirstName, "First name trimmed");

            var found = users.FindByEmailAsync("ANA@example.COM ").GetAwaiter().GetResult();
            Assert.Equal(created.Id, found?.Id, "Found by email regardless of case");

            var duplicate = users.CreateAsync("Other", "Person", "ana@example.com", "hash2").GetAwaiter().GetResult();
            Assert.True(duplicate is null, "Duplicate email rejected");
        }),

        ("Auth: sessions store only a hash, validate, and stop working once revoked", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "ana@example.com");
            var sessions = new SessionService(db.Context);

            var raw = sessions.CreateAsync(user.Id).GetAwaiter().GetResult();
            var stored = db.Context.RefreshTokens.Single();
            Assert.False(stored.TokenHash == raw, "Raw token is not stored");
            Assert.Equal(SessionService.Hash(raw), stored.TokenHash, "SHA-256 hash is stored");

            Assert.Equal(user.Id, sessions.ValidateAsync(raw).GetAwaiter().GetResult(), "Valid session resolves to its user");
            Assert.True(sessions.ValidateAsync(raw + "x").GetAwaiter().GetResult() is null, "Unknown token rejected");

            sessions.RevokeAsync(raw).GetAwaiter().GetResult();
            Assert.True(sessions.ValidateAsync(raw).GetAwaiter().GetResult() is null, "Revoked session rejected");
        }),

        ("Auth: sessions of a deactivated user are rejected", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "ana@example.com");
            var sessions = new SessionService(db.Context);
            var raw = sessions.CreateAsync(user.Id).GetAwaiter().GetResult();

            user.IsActive = false;
            db.Context.SaveChanges();
            Assert.True(sessions.ValidateAsync(raw).GetAwaiter().GetResult() is null, "Inactive user's session rejected");
        }),
    };
}
