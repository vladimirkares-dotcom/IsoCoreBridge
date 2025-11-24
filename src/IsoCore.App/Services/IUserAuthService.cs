using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public interface IUserAuthService
{
    /// <summary>
    /// Attempts to authenticate a user with the given credentials against the local user store.
    /// Returns a result with Success/User/ErrorMessage populated; never throws for bad credentials.
    /// </summary>
    Task<LoginResult> LoginAsync(string username, string password);
    Task LogoutAsync(CancellationToken ct = default);

    UserAccount? CurrentUser { get; }

    Task<bool> IsInRoleAsync(string roleName, CancellationToken ct = default);

    Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken ct = default);
    Task<UserAccount?> GetUserAsync(string username, CancellationToken ct = default);
    IReadOnlyList<UserAccount> GetAllUsers();
    Task<UserAccount> CreateUserAsync(
        UserAccount user,
        string? plainPassword = null,
        CancellationToken cancellationToken = default);

    Task<UserAccount> UpdateUserAsync(
        string originalLogin,
        UserAccount updatedUser,
        string? plainPassword = null,
        CancellationToken cancellationToken = default);

    Task<UserAccount> CreateOrUpdateUserAsync(UserAccount user, string? plainPassword = null);

    Task ToggleActiveAsync(string userId, bool isActive);
    Task DeleteUserAsync(string username);
    Task SetPasswordAsync(string username, string newPassword, CancellationToken ct = default);
    Task ResetPasswordAsync(string username, string newPassword, CancellationToken ct = default);
    Task<UserStoreInfo> GetUserStoreInfoAsync();
}

public sealed record LoginResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    UserAccount? User);

public sealed record UserStoreInfo(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers);
