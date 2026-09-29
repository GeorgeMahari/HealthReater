using System.Text;
using HealthRater.Core.Imaging;
using HealthRater.Data.Services;
using HealthRater.Tests.Framework;

namespace HealthRater.Tests;

public static class ProfileTests
{
    public static List<(string, Action)> All() => new()
    {
        ("Profile: image inspector reads PNG, JPEG and WEBP dimensions", () =>
        {
            Assert.True(ImageInspector.TryInspect(Png(256, 128), out var png), "PNG recognised");
            Assert.Equal((ImageFormat.Png, 256, 128), (png.Format, png.Width, png.Height), "PNG size");
            Assert.True(ImageInspector.TryInspect(Jpeg(300, 200), out var jpg), "JPEG recognised");
            Assert.Equal((ImageFormat.Jpeg, 300, 200), (jpg.Format, jpg.Width, jpg.Height), "JPEG size");
            Assert.True(ImageInspector.TryInspect(WebpLossless(640, 480), out var webp), "WEBP recognised");
            Assert.Equal((ImageFormat.Webp, 640, 480), (webp.Format, webp.Width, webp.Height), "WEBP size");
            Assert.Equal("image/webp", webp.ContentType, "Content type from bytes");
        }),

        ("Profile: image inspector rejects SVG, GIF, executables and garbage", () =>
        {
            Assert.False(ImageInspector.TryInspect(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><script/></svg>"), out _), "SVG");
            Assert.False(ImageInspector.TryInspect(Encoding.ASCII.GetBytes("GIF89a\x01\0\x01\0\0\0\0"), out _), "GIF");
            Assert.False(ImageInspector.TryInspect(new byte[] { 0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0, 4, 0, 0, 0 }, out _), "EXE (MZ)");
            Assert.False(ImageInspector.TryInspect(Png(256, 256).AsSpan(0, 10), out _), "Truncated PNG");
            Assert.False(ImageInspector.TryInspect(Array.Empty<byte>(), out _), "Empty");
        }),

        ("Profile: avatar is stored, validated and removed for the owner only", () =>
        {
            using var db = TestDatabase.Create();
            var alice = TestDatabase.AddUser(db.Context, "alice@example.com");
            var bob = TestDatabase.AddUser(db.Context, "bob@example.com");
            var profiles = new ProfileService(db.Context);

            Assert.True(profiles.SetAvatarAsync(alice.Id, Png(256, 256)).GetAwaiter().GetResult() is null, "Valid PNG accepted");
            Assert.Equal("image/png", profiles.GetAvatarAsync(alice.Id).GetAwaiter().GetResult()?.ContentType, "Stored for Alice");
            Assert.True(profiles.GetAvatarAsync(bob.Id).GetAwaiter().GetResult() is null, "Bob has none");

            Assert.True(profiles.SetAvatarAsync(alice.Id, Encoding.UTF8.GetBytes("<svg/>")).GetAwaiter().GetResult() is not null, "SVG rejected");
            Assert.True(profiles.SetAvatarAsync(alice.Id, Png(8, 8)).GetAwaiter().GetResult() is not null, "Too small rejected");
            Assert.True(profiles.SetAvatarAsync(alice.Id, Png(5000, 5000)).GetAwaiter().GetResult() is not null, "Too large rejected");
            Assert.True(profiles.SetAvatarAsync(alice.Id, new byte[ProfileService.MaxAvatarBytes + 1]).GetAwaiter().GetResult() is not null, "Over 2 MB rejected");

            profiles.RemoveAvatarAsync(alice.Id).GetAwaiter().GetResult();
            Assert.True(profiles.GetAvatarAsync(alice.Id).GetAwaiter().GetResult() is null, "Removed");
            using var read = db.NewContext();
            Assert.True(read.Users.Single(u => u.Id == alice.Id).AvatarUpdatedAt is null, "Avatar marker cleared");
        }),

        ("Profile: editing normalizes email and refuses one that's already taken", () =>
        {
            using var db = TestDatabase.Create();
            var alice = TestDatabase.AddUser(db.Context, "alice@example.com");
            TestDatabase.AddUser(db.Context, "bob@example.com");
            var profiles = new ProfileService(db.Context);

            var (ok, updated) = profiles.UpdateAsync(alice.Id, " Alicia ", "Popa", " ALICIA@Example.com ").GetAwaiter().GetResult();
            Assert.Equal(ProfileUpdateResult.Updated, ok, "Update succeeded");
            Assert.Equal("alicia@example.com", updated!.Email, "Email normalized");
            Assert.Equal("Alicia", updated.FirstName, "Name trimmed");

            var (taken, _) = profiles.UpdateAsync(alice.Id, "Alicia", "Popa", "Bob@example.com").GetAwaiter().GetResult();
            Assert.Equal(ProfileUpdateResult.EmailTaken, taken, "Other user's email refused");
        }),

        ("Profile: changing password can sign out every other session", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var sessions = new SessionService(db.Context);
            var current = sessions.CreateAsync(user.Id).GetAwaiter().GetResult();
            var otherDevice = sessions.CreateAsync(user.Id).GetAwaiter().GetResult();

            sessions.RevokeAllExceptAsync(user.Id, current).GetAwaiter().GetResult();
            Assert.Equal(user.Id, sessions.ValidateAsync(current).GetAwaiter().GetResult(), "Current session kept");
            Assert.True(sessions.ValidateAsync(otherDevice).GetAwaiter().GetResult() is null, "Other device signed out");
        }),

        ("Profile: deleting the account removes its assessments, sessions and avatar", () =>
        {
            using var db = TestDatabase.Create();
            var user = TestDatabase.AddUser(db.Context, "a@example.com");
            var keep = TestDatabase.AddUser(db.Context, "keep@example.com");
            _ = new AssessmentService(db.Context).CreateCompletedAsync(user.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            _ = new AssessmentService(db.Context).CreateCompletedAsync(keep.Id, SampleProfile.Healthy()).GetAwaiter().GetResult().Assessment!;
            new SessionService(db.Context).CreateAsync(user.Id).GetAwaiter().GetResult();
            var profiles = new ProfileService(db.Context);
            profiles.SetAvatarAsync(user.Id, Png(64, 64)).GetAwaiter().GetResult();
            db.Context.ChangeTracker.Clear();

            Assert.True(profiles.DeleteAccountAsync(user.Id).GetAwaiter().GetResult(), "Deleted");
            using var read = db.NewContext();
            Assert.Equal(1, read.Users.Count(), "Only the other user remains");
            Assert.Equal(1, read.HealthAssessments.Count(), "Only the other user's assessment remains");
            Assert.Equal(39, read.AssessmentParameterScores.Count(), "Its 39 parameters remain");
            Assert.Equal(0, read.RefreshTokens.Count(), "Sessions removed");
            Assert.Equal(0, read.UserAvatars.Count(), "Avatar removed");
        }),
    };

    private static byte[] Png(int width, int height)
    {
        var bytes = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 }.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20), height);
        return bytes;
    }

    private static byte[] Jpeg(int width, int height)
    {
        var list = new List<byte> { 0xFF, 0xD8 };
        list.AddRange(new byte[] { 0xFF, 0xE0, 0x00, 0x10 }); // APP0, length 16
        list.AddRange(new byte[14]);
        list.AddRange(new byte[] { 0xFF, 0xC0, 0x00, 0x11, 0x08 }); // SOF0, length 17, precision 8
        list.AddRange(new[] { (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width });
        list.AddRange(new byte[10]);
        return list.ToArray();
    }

    private static byte[] WebpLossless(int width, int height)
    {
        var bytes = new byte[30];
        "RIFF"u8.CopyTo(bytes);
        "WEBP"u8.CopyTo(bytes.AsSpan(8));
        "VP8L"u8.CopyTo(bytes.AsSpan(12));
        bytes[20] = 0x2F;
        var bits = (uint)(width - 1) | ((uint)(height - 1) << 14);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(21), bits);
        return bytes;
    }
}
