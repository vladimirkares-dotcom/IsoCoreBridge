using System;

namespace IsoCore.App.Services;

public static class ProjectStorageManager
{
    private static readonly object LockObject = new();

    public static IProjectStorage? Storage { get; private set; }

    public static void Initialize()
    {
        if (Storage != null)
        {
            return;
        }

        lock (LockObject)
        {
            if (Storage != null)
            {
                return;
            }

            var encryptionService = new EncryptionService();
            Storage = new EncryptedProjectStorageService(encryptionService);
        }
    }
}
