using System;
using System.Security.Cryptography;
using System.Text;

namespace IsoCore.App.Services;

/// <summary>
/// Jednoduch�� SHA-256 hasher pro hesla (do��asn�c �te��en��).
/// Pozd�>ji vym�>n��me za PBKDF2, ale API z��stane stejn�c.
/// </summary>
public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        var computed = HashPassword(password);
        return string.Equals(computed, hash, StringComparison.OrdinalIgnoreCase);
    }
}
