using System.Threading.Tasks;
using IsoCore.Domain;

namespace IsoCore.App.Services.Users;

public interface IUserDirectoryService
{
    string RootPath { get; }
    string UsersPath { get; }
    string AccountsIndexPath { get; }

    void EnsureDirectories();

    Task<UserAccount?> LoadUserByUsernameAsync(string username);
    Task<UserAccount?> LoadUserByIdAsync(string userId);
    Task SaveUserAsync(UserAccount user);
    Task<UserAccount> EnsureDefaultAdminAsync();
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
