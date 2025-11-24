using System;
using System.Collections.Generic;

namespace IsoCore.Domain;

public static class Roles
{
    public const string Administrator = "administrator";
    public const string Mistr = "mistr";
    public const string Technik = "technik";
    public const string Predak = "predak";
    public const string Kontrolor = "kontrolor";
    public const string Delnik = "delnik";

    private const string DefaultDisplayName = "U��ivatel";

    private static readonly Dictionary<string, string> DisplayNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { Administrator, "Administr��tor" },
            { Mistr, "Mistr" },
            { Technik, "Technik" },
            { Predak, "P�ted��k" },
            { Kontrolor, "Kontrolor" },
            { Delnik, "D�>ln��k" }
        };

    public static string GetDisplayName(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return DefaultDisplayName;
        }

        return DisplayNames.TryGetValue(role.Trim(), out var displayName)
            ? displayName
            : DefaultDisplayName;
    }

    public static readonly string[] All =
    {
        Administrator, Mistr, Technik, Predak, Kontrolor, Delnik
    };
}
