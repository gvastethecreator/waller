using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;
using Waller.Native.Core.Topology;
using Waller.Native.Core.Windows;

namespace Waller.Native.Tests;

public sealed partial class CoreArchitectureTests
{

    [Fact]
    public void LocalDataFileSystemErrors_IdentifyRecoverableFileSystemFailures()
    {
        Assert.True(LocalDataFileSystemErrors.IsRecoverable(new IOException("locked")));
        Assert.True(LocalDataFileSystemErrors.IsRecoverable(new UnauthorizedAccessException("denied")));
        Assert.False(LocalDataFileSystemErrors.IsRecoverable(new InvalidOperationException("bug")));
    }

    [Fact]
    public void LocalDataFile_DeleteIfExistsIgnoresMissingFileOrDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-local-file-tests-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "missing", "data.json");

        LocalDataFile.DeleteIfExists(path);
        LocalDataFile.DeleteRecoverableIfExists(path);
        Assert.True(LocalDataFile.TryDeleteIfExists(path));

        Assert.False(Directory.Exists(root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void LocalDataFile_DeleteRejectsBlankPath(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.DeleteIfExists(path!));
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.DeleteRecoverableIfExists(path!));
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.TryDeleteIfExists(path!));
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearDeletesRenderedPngFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            var path = store.CreatePath("DISPLAY-1");
            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            var result = store.Clear();

            Assert.Equal(1, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-root")]
    public void RenderedWallpaperStore_RejectsInvalidRootDirectory(string? rootDirectory)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new RenderedWallpaperStore(rootDirectory!));

        Assert.Equal("rootDirectory", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RenderedWallpaperStore_RejectsBlankMonitorKeyBeforeCreatingDirectory(string? monitorKey)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        var store = new RenderedWallpaperStore(root);

        var error = Assert.ThrowsAny<ArgumentException>(() => store.CreatePath(monitorKey!));

        Assert.Equal("monitorKey", error.ParamName);
        Assert.False(Directory.Exists(store.RenderedDirectory));
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearKeepsNonPngFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var keepPath = Path.Combine(store.RenderedDirectory, "keep.txt");
            await File.WriteAllTextAsync(keepPath, "keep");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.True(File.Exists(keepPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearDeletesRenderedTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var tempPath = Path.Combine(store.RenderedDirectory, ".wallpaper.png.123.tmp");
            await File.WriteAllBytesAsync(tempPath, [1, 2, 3]);

            var result = store.Clear();

            Assert.Equal(1, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(File.Exists(tempPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearKeepsUnrelatedTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var keepPath = Path.Combine(store.RenderedDirectory, "notes.tmp");
            await File.WriteAllTextAsync(keepPath, "keep");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.True(File.Exists(keepPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearReportsFailureWhenRenderedPathIsFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(store.RenderedDirectory, "blocked");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(1, result.Failed);
            Assert.True(result.HasFailures);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void RenderedWallpaperStore_ClearFilesReportsEnumerationFailure()
    {
        static IEnumerable<string> ThrowingFiles()
        {
            yield return @"C:\cache\notes.txt";
            throw new IOException("enumeration blocked");
        }

        var result = RenderedWallpaperStore.ClearFiles(ThrowingFiles());

        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Failed);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public void RenderedWallpaperFileNames_IdentifiesCacheFiles()
    {
        Assert.True(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\wallpaper.png"));
        Assert.True(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\.wallpaper.png.123.tmp"));
        Assert.False(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\notes.tmp"));
        Assert.False(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\notes.txt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RenderedWallpaperFileNames_CreateRejectsBlankMonitorKey(string? monitorKey)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => RenderedWallpaperFileNames.Create(
            monitorKey!,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("monitorKey", error.ParamName);
    }

    [Fact]
    public void RenderedCacheClearResult_ReportsFailures()
    {
        Assert.False(new RenderedCacheClearResult(Deleted: 2, Failed: 0).HasFailures);
        Assert.True(new RenderedCacheClearResult(Deleted: 2, Failed: 1).HasFailures);
    }

    [Theory]
    [InlineData(-1, 0, "Deleted")]
    [InlineData(0, -1, "Failed")]
    public void RenderedCacheClearResult_RejectsNegativeCounts(int deleted, int failed, string parameterName)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RenderedCacheClearResult(deleted, failed));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void RenderedWallpaperStore_SanitizesRenderedFileNames()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            var path = store.CreatePath(@"\\?\DISPLAY#A:B*C?D|E<FGH>");
            var fileName = Path.GetFileName(path);

            Assert.Equal(store.RenderedDirectory, Path.GetDirectoryName(path));
            Assert.Equal(-1, fileName.IndexOfAny(Path.GetInvalidFileNameChars()));
            Assert.EndsWith(".png", fileName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void RenderedWallpaperStore_UsesHashToAvoidSanitizedNameCollisions()
    {
        var first = RenderedWallpaperFileNames.Create("DISPLAY:A", DateTimeOffset.UnixEpoch);
        var second = RenderedWallpaperFileNames.Create("DISPLAY?A", DateTimeOffset.UnixEpoch);

        Assert.NotEqual(first, second);
        Assert.StartsWith("DISPLAY_A_", first);
        Assert.StartsWith("DISPLAY_A_", second);
    }

    [Fact]
    public void RenderedWallpaperStore_BoundsRenderedFileNameLength()
    {
        var monitorKey = new string('A', 300);

        var fileName = RenderedWallpaperFileNames.Create(monitorKey, DateTimeOffset.UnixEpoch);

        Assert.True(fileName.Length < 96);
        Assert.StartsWith(new string('A', 48), fileName);
    }

    [Fact]
    public async Task AtomicFileWriter_WritesNewFileAfterCallbackCompletes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "data.bin");
            byte[] bytes = [1, 2, 3, 4];

            await AtomicFileWriter.WriteAsync(
                path,
                async (stream, token) => await stream.WriteAsync(bytes, token),
                CancellationToken.None);

            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AtomicFileWriter_RejectsInvalidPath(string? path)
    {
        var error = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            AtomicFileWriter.WriteAsync(
                path!,
                (_, _) => Task.CompletedTask,
                CancellationToken.None));

        Assert.Equal("path", error.ParamName);
    }

    [Fact]
    public async Task AtomicFileWriter_RejectsNullWriteCallback()
    {
        Func<Stream, CancellationToken, Task>? write = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AtomicFileWriter.WriteAsync("data.bin", write!, CancellationToken.None));

        Assert.Equal("write", error.ParamName);
    }

    [Fact]
    public async Task AtomicFileWriter_RejectsPathWithoutFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        var path = root + Path.DirectorySeparatorChar;

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            AtomicFileWriter.WriteAsync(
                path,
                (_, _) => Task.CompletedTask,
                CancellationToken.None));

        Assert.Equal("path", error.ParamName);
        Assert.Contains("Atomic write path must include a file name.", error.Message);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void AtomicFileWriter_CreateTempPathUsesTargetDirectoryAndFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "data.bin");

        var tempPath = AtomicFileWriter.CreateTempPath(path);

        Assert.Equal(root, Path.GetDirectoryName(tempPath));
        Assert.StartsWith(".data.bin.", Path.GetFileName(tempPath), StringComparison.Ordinal);
        Assert.EndsWith(".tmp", tempPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AtomicFileWriter_CancelledWriteDoesNotCreateDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "data.bin");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                AtomicFileWriter.WriteAsync(
                    path,
                    (_, _) => Task.CompletedTask,
                    cts.Token));

            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AtomicFileWriter_KeepsExistingFileWhenWriteFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "data.bin");
            await File.WriteAllBytesAsync(path, [9, 8, 7]);

            var error = await Record.ExceptionAsync(() => AtomicFileWriter.WriteAsync(
                path,
                (_, _) => throw new IOException("boom"),
                CancellationToken.None));

            Assert.IsType<IOException>(error);
            Assert.Equal([9, 8, 7], await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
