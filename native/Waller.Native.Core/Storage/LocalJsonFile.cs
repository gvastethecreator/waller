using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Waller.Native.Core.Storage;

internal static class LocalJsonFile
{
    public static async Task<T?> ReadAsync<T>(
        string path,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(
            stream,
            jsonTypeInfo,
            cancellationToken);
    }

    public static async Task<T?> ReadRecoverableAsync<T>(
        string path,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ReadAsync(path, jsonTypeInfo, cancellationToken);
        }
        catch (Exception exception) when (LocalDataReadErrors.IsRecoverable(exception))
        {
            return default;
        }
    }

    public static async Task WriteAsync<T>(
        string path,
        T value,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        await AtomicFileWriter.WriteAsync(
            path,
            (stream, token) => JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, token),
            cancellationToken);
    }
}
