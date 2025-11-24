using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public class EncryptedProjectStorageService : IProjectStorage
{
    private readonly string _storageFilePath;
    private readonly string _legacyJsonPath;
    private readonly EncryptionService _encryptionService;

    public EncryptedProjectStorageService(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));

        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IsoCoreBridge");

        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        _storageFilePath = Path.Combine(baseDir, "projects.encrypted");
        _legacyJsonPath = Path.Combine(baseDir, "projects.json");
    }

    public async Task SaveProjectsAsync(IEnumerable<ProjectInfo> projects)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        using var jsonStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jsonStream, projects, options);
        var plainBytes = jsonStream.ToArray();

        var encryptedBytes = _encryptionService.Encrypt(plainBytes);
        File.WriteAllBytes(_storageFilePath, encryptedBytes);
    }

    public async Task<List<ProjectInfo>> LoadProjectsAsync()
    {
        if (File.Exists(_storageFilePath))
        {
            var encryptedBytes = File.ReadAllBytes(_storageFilePath);

            byte[] plainBytes;
            try
            {
                plainBytes = _encryptionService.Decrypt(encryptedBytes);
            }
            catch (CryptographicException)
            {
                return new List<ProjectInfo>();
            }

            using var jsonStream = new MemoryStream(plainBytes);
            var result = await JsonSerializer.DeserializeAsync<List<ProjectInfo>>(jsonStream);
            return result ?? new List<ProjectInfo>();
        }

        if (File.Exists(_legacyJsonPath))
        {
            try
            {
                await using var jsonStream = File.OpenRead(_legacyJsonPath);
                var legacy = await JsonSerializer.DeserializeAsync<List<ProjectInfo>>(jsonStream)
                             ?? new List<ProjectInfo>();

                await SaveProjectsAsync(legacy);

                return legacy;
            }
            catch
            {
                return new List<ProjectInfo>();
            }
        }

        return new List<ProjectInfo>();
    }
}
