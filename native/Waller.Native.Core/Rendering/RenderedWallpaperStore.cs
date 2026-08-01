using System.Security.Cryptography;
using System.Text;
using Waller.Native.Core.Models;
using Waller.Native.Core.Storage;

namespace Waller.Native.Core.Rendering;

public sealed record RenderedCacheClearResult
{
    public RenderedCacheClearResult(int Deleted, int Failed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Deleted);
        ArgumentOutOfRangeException.ThrowIfNegative(Failed);
        this.Deleted = Deleted;
        this.Failed = Failed;
    }

    public int Deleted { get; }

    public int Failed { get; }

    public static RenderedCacheClearResult Empty { get; } = new(0, 0);

    public static RenderedCacheClearResult Failure() => new(0, 1);

    public bool HasFailures => Failed > 0;
}

public sealed class RenderedWallpaperStore(string rootDirectory)
{
    public string RenderedDirectory { get; } = Path.Combine(
        LocalDataRootDirectory.RequireFullyQualified(rootDirectory),
        "rendered");

    public string CreatePath(string monitorKey)
    {
        var fileName = RenderedWallpaperFileNames.Create(monitorKey, DateTimeOffset.UtcNow);
        EnsureRenderedDirectory();
        return Path.Combine(RenderedDirectory, fileName);
    }

    public RenderedCacheClearResult Clear()
    {
        if (File.Exists(RenderedDirectory))
        {
            return RenderedCacheClearResult.Failure();
        }

        if (!Directory.Exists(RenderedDirectory))
        {
            return RenderedCacheClearResult.Empty;
        }

        return ClearFiles(Directory.EnumerateFiles(RenderedDirectory));
    }

    internal static RenderedCacheClearResult ClearFiles(IEnumerable<string> files)
    {
        var deleted = 0;
        var failed = 0;
        try
        {
            foreach (var file in files.Where(RenderedWallpaperFileNames.IsCacheFile))
            {
                if (LocalDataFile.TryDeleteIfExists(file))
                {
                    deleted++;
                }
                else
                {
                    failed++;
                }
            }
        }
        catch (Exception error) when (LocalDataFileSystemErrors.IsRecoverable(error))
        {
            failed++;
        }

        return new RenderedCacheClearResult(deleted, failed);
    }

    private void EnsureRenderedDirectory() => Directory.CreateDirectory(RenderedDirectory);
}

internal static class RenderedWallpaperFileNames
{
    private const int MaxPrefixLength = 48;
    private const int HashLength = 12;

    public static string Create(string monitorKey, DateTimeOffset createdAt)
    {
        var key = MonitorKeys.Require(monitorKey, nameof(monitorKey));
        var safePrefix = SafePrefix(key);
        var hash = ShortHash(key);
        return $"{safePrefix}_{hash}_{createdAt:yyyyMMddHHmmssfff}.png";
    }

    public static bool IsCacheFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
            || IsInternalTempFile(path);
    }

    private static bool IsInternalTempFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.StartsWith(".", StringComparison.Ordinal)
            && fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            && fileName.Contains(".png.", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafePrefix(string monitorKey)
    {
        if (string.IsNullOrWhiteSpace(monitorKey))
        {
            return "monitor";
        }

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(monitorKey.Length);
        var lastWasSeparator = false;

        foreach (var character in monitorKey)
        {
            var next = invalid.Contains(character) || char.IsWhiteSpace(character)
                ? '_'
                : character;
            if (next == '_')
            {
                if (lastWasSeparator)
                {
                    continue;
                }

                lastWasSeparator = true;
            }
            else
            {
                lastWasSeparator = false;
            }

            builder.Append(next);
            if (builder.Length == MaxPrefixLength)
            {
                break;
            }
        }

        var prefix = builder.ToString().Trim('_', '.');
        return string.IsNullOrWhiteSpace(prefix) ? "monitor" : prefix;
    }

    private static string ShortHash(string monitorKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(monitorKey));
        return Convert.ToHexString(hash)[..HashLength].ToLowerInvariant();
    }
}
