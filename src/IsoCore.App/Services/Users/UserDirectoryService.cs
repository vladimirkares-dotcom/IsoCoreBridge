using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.Services.Users;

/// <summary>
/// Local-only user directory helper for storing individual user files and an index mapping logins to user ids.
/// </summary>
public class UserDirectoryService : IUserDirectoryService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string RootPath { get; }
    public string UsersPath { get; }
    public string AccountsIndexPath { get; }

    private sealed class AccountIndex
    {
        [JsonPropertyName("accounts")]
        public List<AccountEntry> Accounts { get; set; } = new();
    }

    private sealed class AccountEntry
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
    }

    public UserDirectoryService()
    {
        RootPath = InitializeRootPath();
        UsersPath = Path.Combine(RootPath, "users");
        AccountsIndexPath = Path.Combine(RootPath, "auth", "accounts.json");
    }

    public void EnsureDirectories()
    {
        TryCreateDirectory(UsersPath);
        var authDir = Path.GetDirectoryName(AccountsIndexPath);
        if (!string.IsNullOrWhiteSpace(authDir))
        {
            TryCreateDirectory(authDir);
        }
    }

    public async Task<UserAccount?> LoadUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var index = await LoadAccountIndexAsync().ConfigureAwait(false);
        var entry = index.Accounts
            .FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

        return entry == null ? null : await LoadUserByIdAsync(entry.UserId).ConfigureAwait(false);
    }

    public async Task<UserAccount?> LoadUserByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var file = Path.Combine(UsersPath, $"{userId}.json");
        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(file);
            return await JsonSerializer.DeserializeAsync<UserAccount>(stream, _jsonOptions).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveUserAsync(UserAccount user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        EnsureDirectories();

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            user.Id = Guid.NewGuid().ToString("D");
        }

        var file = Path.Combine(UsersPath, $"{user.Id}.json");

        try
        {
            await using (var stream = File.Create(file))
            {
                await JsonSerializer.SerializeAsync(stream, user, _jsonOptions).ConfigureAwait(false);
            }

            var index = await LoadAccountIndexAsync().ConfigureAwait(false);
            var existing = index.Accounts.FirstOrDefault(a =>
                a.UserId == user.Id || string.Equals(a.Username, user.Username, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                index.Accounts.Add(new AccountEntry
                {
                    Username = user.Username,
                    UserId = user.Id
                });
            }
            else
            {
                existing.Username = user.Username;
                existing.UserId = user.Id;
            }

            await SaveAccountIndexAsync(index).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UserDirectoryService: failed to save user '{user.Username}': {ex}");
        }
    }

    public async Task<UserAccount> EnsureDefaultAdminAsync()
    {
        EnsureDirectories();

        var index = await LoadAccountIndexAsync().ConfigureAwait(false);
        var existing = index.Accounts.FirstOrDefault(a =>
            string.Equals(a.Username, "admin", StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            var existingUser = await LoadUserByIdAsync(existing.UserId).ConfigureAwait(false);
            if (existingUser != null)
            {
                return existingUser;
            }
        }

        var admin = new UserAccount
        {
            Username = "admin",
            Login = "admin",
            DisplayName = "Administrator",
            Role = Roles.Administrator,
            IsActive = true,
            PasswordHash = PasswordHasher.HashPassword("admin"),
            PasswordSalt = string.Empty
        };

        await SaveUserAsync(admin).ConfigureAwait(false);
        return admin;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var record = await LoadUserByUsernameAsync(username).ConfigureAwait(false);
        if (record == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(record.PasswordHash))
        {
            return false;
        }

        return PasswordHasher.VerifyPassword(password, record.PasswordHash);
    }

    private async Task<AccountIndex> LoadAccountIndexAsync()
    {
        EnsureDirectories();

        if (!File.Exists(AccountsIndexPath))
        {
            return new AccountIndex();
        }

        try
        {
            await using var stream = File.OpenRead(AccountsIndexPath);
            var result = await JsonSerializer.DeserializeAsync<AccountIndex>(stream, _jsonOptions)
                .ConfigureAwait(false);
            return result ?? new AccountIndex();
        }
        catch
        {
            return new AccountIndex();
        }
    }

    private async Task SaveAccountIndexAsync(AccountIndex index)
    {
        EnsureDirectories();

        try
        {
            await using var stream = File.Create(AccountsIndexPath);
            await JsonSerializer.SerializeAsync(stream, index, _jsonOptions).ConfigureAwait(false);
        }
        catch
        {
            // best-effort; keep local state unchanged if write fails
        }
    }

    private static string InitializeRootPath()
    {
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IsoCoreBridge");
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "IsoCoreBridge");
        }
    }

    private static void TryCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch
        {
            // best-effort, stay silent
        }
    }
}
