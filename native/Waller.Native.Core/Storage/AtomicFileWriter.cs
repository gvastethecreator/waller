namespace Waller.Native.Core.Storage;

internal static class AtomicFileWriter
{
    public static async Task WriteAsync(
        string path,
        Func<Stream, CancellationToken, Task> write,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(write);

        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(path);
        var tempPath = CreateTempPath(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await write(stream, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            LocalDataFile.DeleteRecoverableIfExists(tempPath);
        }
    }

    internal static string CreateTempPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileName = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Atomic write path must include a file name.", nameof(path));
        }

        var directory = Path.GetDirectoryName(path);
        return Path.Combine(
            directory ?? string.Empty,
            $".{fileName}.{Guid.NewGuid():N}.tmp");
    }
}
