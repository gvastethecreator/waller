using System.Security.Cryptography;
using System.Text;

namespace Waller.Native.Core.Rendering;

public sealed record RenderedCacheClearResult(int Deleted, int Failed)
{
    public static RenderedCacheClearResult Empty { get; } = new(0, 0);

    public static RenderedCacheClearResult Failure() => new(0, 1);

    public bool HasFailures => Failed > 0;
}

public sealed class RenderedWallpaperStore(string rootDirectory)
{
    public string RenderedDirectory { get; } = Path.Combine(rootDirectory, "rendered");

    public string CreatePath(string monitorKey)
    {
        EnsureRenderedDirectory();
        return Path.Combine(RenderedDirectory, RenderedWallpaperFileNames.Create(monitorKey, DateTimeOffset.UtcNow));
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
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (IOException)
                {
                    failed++;
                }
                catch (UnauthorizedAccessException)
                {
                    failed++;
                }
            }
        }
        catch (IOException)
        {
            failed++;
        }
        catch (UnauthorizedAccessException)
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
        var key = monitorKey ?? string.Empty;
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
