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

namespace IsoCore.App.Services.Projects;

public class ProjectsStorageService : IProjectsStorageService
{
    private readonly string _projectsFilePath;
    private readonly EncryptionService? _encryptionService;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ProjectsStorageService()
    {
        _projectsFilePath = InitializeProjectsFilePath();
        _encryptionService = TryCreateEncryptionService();
    }

    public async Task<IReadOnlyList<ProjectInfo>> LoadProjectsAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_projectsFilePath))
            {
                return Array.Empty<ProjectInfo>();
            }

            try
            {
                var blob = await ReadFileSafeAsync(_projectsFilePath, cancellationToken).ConfigureAwait(false);
                if (blob.Length == 0)
                {
                    return Array.Empty<ProjectInfo>();
                }

                var plainBytes = DecryptBytes(blob);
                if (plainBytes.Length == 0)
                {
                    return Array.Empty<ProjectInfo>();
                }

                var json = Encoding.UTF8.GetString(plainBytes);
                var db = JsonSerializer.Deserialize<ProjectsStoreModel>(json, SerializerOptions);
                return (db?.Projects ?? new List<ProjectInfo>()).ToList();
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine("ProjectsStorageService: invalid or incompatible projects store; resetting to empty.");
                BackupCorruptedStore();
                await WriteStoreAsync(Array.Empty<ProjectInfo>(), cancellationToken).ConfigureAwait(false);
                return Array.Empty<ProjectInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProjectsStorageService: unexpected load failure, returning empty list: {ex}");
                return Array.Empty<ProjectInfo>();
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveProjectsAsync(IEnumerable<ProjectInfo> projects, CancellationToken cancellationToken = default)
    {
        if (projects == null)
        {
            throw new ArgumentNullException(nameof(projects));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TryEnsureProjectsDirectory();

            await WriteStoreAsync(projects, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static string InitializeProjectsFilePath()
    {
        var fallbackDir = Path.GetTempPath();
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IsoCoreBridge");
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "projects.encrypted");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProjectsStorageService: failed to initialize app data path, falling back to temp: {ex}");
            try
            {
                Directory.CreateDirectory(fallbackDir);
            }
            catch
            {
                // ignore; temp path should exist
            }

            return Path.Combine(fallbackDir, "IsoCoreBridge_projects.encrypted");
        }
    }

    private void TryEnsureProjectsDirectory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_projectsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProjectsStorageService: failed to create projects directory: {ex}");
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
            Debug.WriteLine($"ProjectsStorageService: encryption unavailable, falling back to plaintext store: {ex}");
            return null;
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
            Debug.WriteLine($"ProjectsStorageService: encryption failed, writing plaintext: {ex}");
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
            Debug.WriteLine($"ProjectsStorageService: decryption failed, ignoring stored projects: {ex}");
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
            Debug.WriteLine($"ProjectsStorageService: failed to read projects store: {ex}");
            return Array.Empty<byte>();
        }
    }

    private async Task WriteStoreAsync(IEnumerable<ProjectInfo> projects, CancellationToken ct)
    {
        var db = new ProjectsStoreModel
        {
            Projects = projects.ToList()
        };

        var json = JsonSerializer.Serialize(db, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var encrypted = EncryptBytes(bytes);
        await File.WriteAllBytesAsync(_projectsFilePath, encrypted, ct).ConfigureAwait(false);
    }

    private void BackupCorruptedStore()
    {
        try
        {
            if (!File.Exists(_projectsFilePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(_projectsFilePath);
            var fileName = Path.GetFileNameWithoutExtension(_projectsFilePath);
            var extension = Path.GetExtension(_projectsFilePath);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var backupName = $"{fileName}_{timestamp}.bak{extension}";
            var backupPath = string.IsNullOrWhiteSpace(directory)
                ? backupName
                : Path.Combine(directory, backupName);

            File.Move(_projectsFilePath, backupPath, overwrite: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProjectsStorageService: failed to backup corrupted store: {ex}");
        }
    }

    private sealed class ProjectsStoreModel
    {
        public List<ProjectInfo> Projects { get; set; } = new();
    }
}
