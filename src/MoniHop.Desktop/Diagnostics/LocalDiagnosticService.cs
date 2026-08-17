using System.IO;
using System.Text;
using System.Text.Json;

namespace MoniHop.Desktop.Diagnostics;

public sealed record DiagnosticSnapshot(
    string ProductVersion,
    string OperatingSystem,
    string ProcessArchitecture,
    string RunIdentity,
    int ConnectedDisplayCount,
    bool ConfigurationLoaded);

public sealed class LocalDiagnosticService
{
    private const long MaximumLogBytes = 1024 * 1024;
    private const int MaximumMessageCharacters = 4096;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _logPath;
    private readonly Func<bool> _isEnabled;
    private readonly object _sync = new();
    private bool _hasWriteFailure;

    public LocalDiagnosticService(string logPath, Func<bool> isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
        _logPath = logPath;
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
    }

    public string LogPath => _logPath;

    public bool HasWriteFailure
    {
        get
        {
            lock (_sync)
            {
                return _hasWriteFailure;
            }
        }
    }

    public long LogSize
    {
        get
        {
            lock (_sync)
            {
                try
                {
                    return File.Exists(_logPath) ? new FileInfo(_logPath).Length : 0;
                }
                catch (IOException)
                {
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    return 0;
                }
            }
        }
    }

    public void Write(string eventName, string message, bool always = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(message);
        lock (_sync)
        {
            try
            {
                if (!always && !_isEnabled())
                {
                    return;
                }

                var directory = Path.GetDirectoryName(_logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

            var normalizedMessage = message.ReplaceLineEndings(" ");
            if (normalizedMessage.Length > MaximumMessageCharacters)
            {
                normalizedMessage = normalizedMessage[..MaximumMessageCharacters];
            }

                File.AppendAllText(
                    _logPath,
                    $"{DateTimeOffset.Now:O}\t{eventName}\t{normalizedMessage}{Environment.NewLine}");
                TrimLogIfNeeded();
            }
            catch (IOException)
            {
                _hasWriteFailure = true;
            }
            catch (UnauthorizedAccessException)
            {
                _hasWriteFailure = true;
            }
            catch (ArgumentException)
            {
                _hasWriteFailure = true;
            }
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            if (File.Exists(_logPath))
            {
                File.Delete(_logPath);
            }
        }
    }

    public void Export(string path, DiagnosticSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(snapshot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string[] recentEntries;
        lock (_sync)
        {
            recentEntries = File.Exists(_logPath)
                ? File.ReadLines(_logPath).TakeLast(200).ToArray()
                : [];
        }

        var report = new
        {
            GeneratedAt = DateTimeOffset.Now,
            Snapshot = snapshot,
            RecentDiagnosticEntries = recentEntries,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report, SerializerOptions));
    }

    private void TrimLogIfNeeded()
    {
        if (new FileInfo(_logPath).Length <= MaximumLogBytes)
        {
            return;
        }

        var targetBytes = MaximumLogBytes / 2;
        var retained = new List<string>();
        var retainedBytes = 0;
        foreach (var line in File.ReadLines(_logPath).Reverse())
        {
            var lineBytes = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
            if (retained.Count > 0 && retainedBytes + lineBytes > targetBytes)
            {
                break;
            }

            retained.Add(line);
            retainedBytes += lineBytes;
        }

        retained.Reverse();
        File.WriteAllLines(_logPath, retained, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
