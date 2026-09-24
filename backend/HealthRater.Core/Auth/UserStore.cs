using System.Text.Json;

namespace HealthRater.Core.Auth;

public class UserRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Always stored normalized (trimmed, lower-case).</summary>
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public interface IUserStore
{
    Task<UserRecord?> FindByEmailAsync(string email);
    Task<UserRecord?> FindByIdAsync(Guid id);
    /// <summary>Returns null when the email is already registered.</summary>
    Task<UserRecord?> CreateAsync(string name, string email, string passwordHash);
}

/// <summary>
/// Stores users in a single JSON file. Writes go to a temp file first and are then
/// moved over the original so a crash mid-write can't leave a truncated file.
/// Suitable for a single API instance; swap for a database-backed IUserStore later.
/// </summary>
public class JsonFileUserStore : IUserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<UserRecord>? _users;

    public JsonFileUserStore(string path)
    {
        _path = path;
    }

    public async Task<UserRecord?> FindByEmailAsync(string email)
    {
        var normalized = AuthValidator.NormalizeEmail(email);
        await _lock.WaitAsync();
        try
        {
            return (await LoadAsync()).FirstOrDefault(u => u.Email == normalized);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<UserRecord?> FindByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            return (await LoadAsync()).FirstOrDefault(u => u.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<UserRecord?> CreateAsync(string name, string email, string passwordHash)
    {
        var normalized = AuthValidator.NormalizeEmail(email);
        await _lock.WaitAsync();
        try
        {
            var users = await LoadAsync();
            if (users.Any(u => u.Email == normalized)) return null;

            var user = new UserRecord
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Email = normalized,
                PasswordHash = passwordHash,
                CreatedAtUtc = DateTime.UtcNow,
            };
            users.Add(user);
            await SaveAsync(users);
            return user;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<UserRecord>> LoadAsync()
    {
        if (_users is not null) return _users;
        if (!File.Exists(_path)) return _users = new List<UserRecord>();

        await using var stream = File.OpenRead(_path);
        _users = await JsonSerializer.DeserializeAsync<List<UserRecord>>(stream, JsonOptions) ?? new List<UserRecord>();
        return _users;
    }

    private async Task SaveAsync(List<UserRecord> users)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
        }
        File.Move(temp, _path, overwrite: true);
    }
}
