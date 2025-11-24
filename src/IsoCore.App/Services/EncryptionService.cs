using System;
using System.IO;
using System.Security.Cryptography;

namespace IsoCore.App.Services;

public class EncryptionService
{
    private readonly string _keyFilePath;

    public EncryptionService()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IsoCoreBridge");

        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        _keyFilePath = Path.Combine(baseDir, "key.bin");
    }

    /// <summary>
    /// Returns the 256-bit key, creating and protecting it via DPAPI if it is missing.
    /// </summary>
    private byte[] GetOrCreateKey()
    {
        if (File.Exists(_keyFilePath))
        {
            var protectedBytes = File.ReadAllBytes(_keyFilePath);
            return ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        }

        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var protectedKey = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_keyFilePath, protectedKey);

        return key;
    }

    public byte[] Encrypt(byte[] plainBytes)
    {
        var key = GetOrCreateKey();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    public byte[] Decrypt(byte[] cipherBytes)
    {
        var key = GetOrCreateKey();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(cipherBytes);

        var iv = new byte[aes.BlockSize / 8];
        var read = ms.Read(iv, 0, iv.Length);
        if (read != iv.Length)
        {
            throw new InvalidOperationException("Encrypted data is corrupted (IV missing).");
        }

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var resultStream = new MemoryStream();
        cryptoStream.CopyTo(resultStream);

        return resultStream.ToArray();
    }
}
