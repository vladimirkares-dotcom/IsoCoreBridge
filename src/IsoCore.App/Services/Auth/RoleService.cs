using System;
using System.Collections.Generic;
using System.Linq;
using IsoCore.App.ViewModels;
using IsoCore.Domain;

namespace IsoCore.App.Services.Auth;

public class RoleService : IRoleService
{
    private readonly IReadOnlyList<RoleOption> _roleOptions;

    public RoleService()
    {
        _roleOptions = BuildRoleOptions();
    }

    public IReadOnlyList<RoleOption> GetAllRoles() => _roleOptions;

    public RoleOption GetDefaultRole()
    {
        const string defaultRoleId = Roles.Delnik;
        return GetRoleById(defaultRoleId) ?? new RoleOption(defaultRoleId, Roles.GetDisplayName(defaultRoleId));
    }

    public RoleOption? GetRoleById(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return null;
        }

        return _roleOptions.FirstOrDefault(r =>
            string.Equals(r.Value, roleId, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<RoleOption> BuildRoleOptions()
    {
        return Roles.All
            .Select(role => new RoleOption(role, Roles.GetDisplayName(role)))
            .ToList();
    }
}
