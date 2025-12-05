using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.Services.Auth;

public class JsonUserAuthService : IUserAuthService
{
    private readonly EncryptionService? _encryptionService;
    private readonly string _usersFilePath;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, UserAccount> _users = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public JsonUserAuthService()
    {
        _usersFilePath = InitializeUsersFilePath();
        _encryptionService = TryCreateEncryptionService();
    }

    public UserAccount? CurrentUser { get; private set; }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        CurrentUser = null;

        try
        {
            await EnsureUsersLoadedAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(false, "InvalidCredentials", "Neplatné přihlašovací údaje.", null);
            }

            UserAccount? user;
            await _mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                var key = username.Trim();
                user = _users.GetValueOrDefault(key)
                    ?? _users.Values.FirstOrDefault(u =>
                        string.Equals(u.Login, key, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _mutex.Release();
            }

            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                return new LoginResult(false, "InvalidCredentials", "Neplatné přihlašovací údaje.", null);
            }

            if (!user.IsActive)
            {
                return new LoginResult(false, "InactiveAccount", "Uživatelský účet je neaktivní.", user);
            }

            CurrentUser = user;

            if (user.MustChangePassword)
            {
                return new LoginResult(false, "MustChangePassword", "Je vyžadována změna hesla.", user);
            }

            return new LoginResult(true, null, null, user);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: login failed due to store error: {ex}");
            return new LoginResult(false, "UserStoreError", "Nelze načíst uživatelská data.", null);
        }
    }

    public Task LogoutAsync(CancellationToken ct = default)
    {
        CurrentUser = null;
        return Task.CompletedTask;
    }

    public async Task<bool> IsInRoleAsync(string roleName, CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CurrentUser == null || string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        return string.Equals(CurrentUser.Role, roleName, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken ct = default)
    {
        await EnsureUsersLoadedAsync(ct).ConfigureAwait(false);
        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return _users.Values.ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserAccount?> GetUserAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        await EnsureUsersLoadedAsync(ct).ConfigureAwait(false);
        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return _users.GetValueOrDefault(username.Trim());
        }
        finally
        {
            _mutex.Release();
        }
    }

    public IReadOnlyList<UserAccount> GetAllUsers()
    {
        EnsureUsersLoadedAsync().GetAwaiter().GetResult();
        _mutex.Wait();
        try
        {
            return _users.Values.ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserAccount> CreateUserAsync(
        UserAccount user,
        string? plainPassword = null,
        CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        await EnsureUsersLoadedAsync(cancellationToken).ConfigureAwait(false);
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var username = NormalizeUsername(user.Username);
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username is required.", nameof(user));
            }

            if (_users.ContainsKey(username))
            {
                throw new InvalidOperationException($"User with username '{username}' already exists.");
            }

            var persisted = CloneUser(user);
            if (string.IsNullOrWhiteSpace(persisted.Id))
            {
                persisted.Id = Guid.NewGuid().ToString("D");
            }

            persisted.Username = username;
            persisted.Login = string.IsNullOrWhiteSpace(persisted.Login) ? username : persisted.Login.Trim();
            SetPasswordHash(persisted, plainPassword);

            _users[username] = persisted;
            await SaveUsersLockedAsync(cancellationToken).ConfigureAwait(false);
            return persisted;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserAccount> UpdateUserAsync(
        string originalLogin,
        UserAccount updatedUser,
        string? plainPassword = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalLogin))
        {
            throw new ArgumentException("Original login is required.", nameof(originalLogin));
        }

        if (updatedUser == null)
        {
            throw new ArgumentNullException(nameof(updatedUser));
        }

        await EnsureUsersLoadedAsync(cancellationToken).ConfigureAwait(false);
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var originalKey = NormalizeUsername(originalLogin);
            if (!_users.TryGetValue(originalKey, out var existing))
            {
                throw new InvalidOperationException($"User '{originalLogin}' does not exist.");
            }

            var newKey = NormalizeUsername(
                string.IsNullOrWhiteSpace(updatedUser.Username) ? updatedUser.Login : updatedUser.Username);

            if (string.IsNullOrWhiteSpace(newKey))
            {
                throw new ArgumentException("Username is required.", nameof(updatedUser));
            }

            if (!string.Equals(originalKey, newKey, StringComparison.OrdinalIgnoreCase) &&
                _users.ContainsKey(newKey))
            {
                throw new InvalidOperationException($"User with username '{newKey}' already exists.");
            }

            existing.DisplayName = updatedUser.DisplayName;
            existing.Role = updatedUser.Role;
            existing.IsActive = updatedUser.IsActive;
            existing.WorkerNumber = updatedUser.WorkerNumber;
            existing.CompanyName = updatedUser.CompanyName;
            existing.CompanyAddress = updatedUser.CompanyAddress;
            existing.PhoneNumber = updatedUser.PhoneNumber;
            existing.TitleBefore = updatedUser.TitleBefore;
            existing.TitleAfter = updatedUser.TitleAfter;
            existing.JobTitle = updatedUser.JobTitle;
            existing.Note = updatedUser.Note;
            existing.MustChangePassword = updatedUser.MustChangePassword;
            existing.FirstName = updatedUser.FirstName;
            existing.LastName = updatedUser.LastName;
            existing.EmploymentType = updatedUser.EmploymentType;
            existing.Login = string.IsNullOrWhiteSpace(updatedUser.Login) ? newKey : updatedUser.Login.Trim();
            existing.Username = newKey;

            if (!string.IsNullOrWhiteSpace(plainPassword))
            {
                SetPasswordHash(existing, plainPassword);
            }

            if (!string.Equals(originalKey, newKey, StringComparison.OrdinalIgnoreCase))
            {
                _users.Remove(originalKey);
                _users[newKey] = existing;
            }

            await SaveUsersLockedAsync(cancellationToken).ConfigureAwait(false);
            return existing;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserAccount> CreateOrUpdateUserAsync(UserAccount user, string? plainPassword = null)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var username = NormalizeUsername(user.Username);
        await EnsureUsersLoadedAsync().ConfigureAwait(false);

        var exists = false;
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            exists = _users.ContainsKey(username);
        }
        finally
        {
            _mutex.Release();
        }

        return exists
            ? await UpdateUserAsync(username, user, plainPassword).ConfigureAwait(false)
            : await CreateUserAsync(user, plainPassword).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(string userId, bool isActive)
    {
        await EnsureUsersLoadedAsync().ConfigureAwait(false);
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var target = _users.Values.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return;
            }

            target.IsActive = isActive;
            await SaveUsersLockedAsync().ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteUserAsync(string username)
    {
        await EnsureUsersLoadedAsync().ConfigureAwait(false);
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_users.Remove(NormalizeUsername(username)))
            {
                await SaveUsersLockedAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SetPasswordAsync(string username, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("Password is required.", nameof(newPassword));
        }

        await EnsureUsersLoadedAsync(ct).ConfigureAwait(false);
        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var key = NormalizeUsername(username);
            if (!_users.TryGetValue(key, out var user))
            {
                throw new InvalidOperationException("User not found.");
            }

            SetPasswordHash(user, newPassword);
            user.MustChangePassword = false;
            await SaveUsersLockedAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task ResetPasswordAsync(string username, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("Password is required.", nameof(newPassword));
        }

        await EnsureUsersLoadedAsync(ct).ConfigureAwait(false);
        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var key = NormalizeUsername(username);
            if (!_users.TryGetValue(key, out var user))
            {
                throw new InvalidOperationException("User not found.");
            }

            SetPasswordHash(user, newPassword);
            user.MustChangePassword = true;
            await SaveUsersLockedAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserStoreInfo> GetUserStoreInfoAsync()
    {
        await EnsureUsersLoadedAsync().ConfigureAwait(false);
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var total = _users.Count;
            var active = _users.Values.Count(u => u.IsActive);
            var inactive = total - active;
            return new UserStoreInfo(total, active, inactive);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task EnsureUsersLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded)
        {
            return;
        }

        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            try
            {
                if (_loaded)
                {
                    return;
                }

                TryEnsureUsersDirectory();

                if (!File.Exists(_usersFilePath))
                {
                    var admin = CreateDefaultAdmin();
                    _users[admin.Username] = admin;
                    await SaveUsersLockedAsync(ct).ConfigureAwait(false);
                    _loaded = true;
                    return;
                }

                var blob = await ReadFileSafeAsync(_usersFilePath, ct).ConfigureAwait(false);
                if (blob.Length == 0)
                {
                    _loaded = true;
                    return;
                }

                var plainBytes = DecryptBytes(blob);
                if (plainBytes.Length == 0)
                {
                    _loaded = true;
                    return;
                }

                var json = Encoding.UTF8.GetString(plainBytes);
                var db = JsonSerializer.Deserialize<UserDatabase>(json, SerializerOptions);
                var users = db?.Users ?? new List<UserAccount>();

                _users.Clear();
                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.Username))
                    {
                        _users[NormalizeUsername(user.Username)] = user;
                    }
                }
            }
            catch
            {
                _users.Clear();
                var admin = CreateDefaultAdmin();
                _users[admin.Username] = admin;
                await SaveUsersLockedAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                _loaded = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: failed to load users store: {ex}");
            _users.Clear();
            _loaded = true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task SaveUsersLockedAsync(CancellationToken ct = default)
    {
        var db = new UserDatabase
        {
            Users = _users.Values.ToList()
        };

        var json = JsonSerializer.Serialize(db, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var encrypted = EncryptBytes(bytes);
        try
        {
            await File.WriteAllBytesAsync(_usersFilePath, encrypted, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: failed to write users store: {ex}");
        }
    }

    private static UserAccount CreateDefaultAdmin()
    {
        var passwordHash = PasswordHasher.HashPassword("admin");

        return new UserAccount
        {
            Id = Guid.NewGuid().ToString("D"),
            Username = "admin",
            Login = "admin",
            DisplayName = "Administrator",
            Role = Roles.Administrator,
            IsActive = true,
            PasswordHash = passwordHash,
            PasswordSalt = string.Empty,
            MustChangePassword = false
        };
    }

    private static void SetPasswordHash(UserAccount user, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        user.PasswordHash = PasswordHasher.HashPassword(password);
        user.PasswordSalt = string.Empty;
    }

    private static string NormalizeUsername(string? username)
    {
        return string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim();
    }

    private static UserAccount CloneUser(UserAccount source)
    {
        return new UserAccount
        {
            Id = source.Id,
            Username = source.Username,
            DisplayName = source.DisplayName,
            Login = source.Login,
            WorkerNumber = source.WorkerNumber,
            TitleBefore = source.TitleBefore,
            FirstName = source.FirstName,
            LastName = source.LastName,
            TitleAfter = source.TitleAfter,
            JobTitle = source.JobTitle,
            CompanyName = source.CompanyName,
            CompanyAddress = source.CompanyAddress,
            PhoneNumber = source.PhoneNumber,
            Role = source.Role,
            IsActive = source.IsActive,
            Note = source.Note,
            EmploymentType = source.EmploymentType,
            MustChangePassword = source.MustChangePassword,
            PasswordHash = source.PasswordHash,
            PasswordSalt = source.PasswordSalt
        };
    }

    private sealed class UserDatabase
    {
        public List<UserAccount> Users { get; set; } = new();
    }

    private static string InitializeUsersFilePath()
    {
        var fallbackDir = Path.GetTempPath();
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IsoCoreBridge");
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "users.encrypted");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: failed to initialize app data path, falling back to temp: {ex}");
            try
            {
                Directory.CreateDirectory(fallbackDir);
            }
            catch
            {
                // swallow; Path.GetTempPath should exist
            }

            return Path.Combine(fallbackDir, "IsoCoreBridge_users.encrypted");
        }
    }

    private EncryptionService? TryCreateEncryptionService()
    {
        try
        {
            return new EncryptionService();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: encryption unavailable, falling back to plaintext store: {ex}");
            return null;
        }
    }

    private void TryEnsureUsersDirectory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_usersFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: failed to create users directory: {ex}");
        }
    }

    private byte[] EncryptBytes(byte[] bytes)
    {
        if (_encryptionService == null)
        {
            return bytes;
        }

        try
        {
            return _encryptionService.Encrypt(bytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: encryption failed, writing plaintext: {ex}");
            return bytes;
        }
    }

    private byte[] DecryptBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return bytes;
        }

        if (_encryptionService == null)
        {
            return bytes;
        }

        try
        {
            return _encryptionService.Decrypt(bytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: decryption failed, ignoring stored users: {ex}");
            return Array.Empty<byte>();
        }
    }

    private static async Task<byte[]> ReadFileSafeAsync(string path, CancellationToken ct)
    {
        try
        {
            return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JsonUserAuthService: failed to read users store: {ex}");
            return Array.Empty<byte>();
        }
    }
}
