using System.Collections.Generic;
using IsoCore.App.ViewModels;

namespace IsoCore.App.Services.Auth;

public interface IRoleService
{
    IReadOnlyList<RoleOption> GetAllRoles();

    RoleOption GetDefaultRole();

    RoleOption? GetRoleById(string roleId);
}
