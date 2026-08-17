using System.IO;

namespace MoniHop.Desktop.Settings;

internal static class JsonStoreRecovery
{
    private static int _recoveryCount;

    public static int RecoveryCount => Volatile.Read(ref _recoveryCount);

    public static string QuarantineCorruptFile(string path)
    {
        var backupPath = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json";
        File.Move(path, backupPath);
        Interlocked.Increment(ref _recoveryCount);
        return backupPath;
    }
}
