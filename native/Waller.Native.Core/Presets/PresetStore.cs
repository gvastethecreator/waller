using Waller.Native.Core.Models;
using Waller.Native.Core.Serialization;
using Waller.Native.Core.Storage;

namespace Waller.Native.Core.Presets;

public sealed class PresetStore(string rootDirectory)
{
    private const string PresetFileSearchPattern = "*.json";

    private readonly string presetsDirectory = Path.Combine(
        LocalDataRootDirectory.RequireFullyQualified(rootDirectory),
        "presets");

    public async Task<IReadOnlyList<Preset>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var presets = new List<Preset>();

            foreach (var path in Directory.EnumerateFiles(presetsDirectory, PresetFileSearchPattern))
            {
                var preset = await LoadFromPathAsync(path, cancellationToken);
                if (preset is not null)
                {
                    presets.Add(preset);
                }
            }

            return presets
                .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(preset => preset.Id)
                .ToList();
        }
        catch (Exception exception) when (LocalDataReadErrors.IsRecoverableFileSystem(exception))
        {
            return [];
        }
    }

    public async Task<Preset?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var path = GetPath(id);
            return await LoadFromPathAsync(path, cancellationToken);
        }
        catch (Exception exception) when (LocalDataReadErrors.IsRecoverableFileSystem(exception))
        {
            return null;
        }
    }

    public async Task<Preset> SaveAsync(Preset preset, CancellationToken cancellationToken = default)
    {
        var normalized = PresetFilePolicy.NormalizeForSave(preset, DateTimeOffset.UtcNow);
        cancellationToken.ThrowIfCancellationRequested();
        EnsurePresetsDirectory();

        var path = GetPath(normalized.Id);
        await LocalJsonFile.WriteAsync(
            path,
            normalized,
            WallerJsonContext.Default.Preset,
            cancellationToken);
        return normalized;
    }

    public async Task<Preset> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var preset = await LoadAsync(id, cancellationToken)
            ?? throw new FileNotFoundException($"Preset not found: {id}");

        return await SaveAsync(PresetFactory.Rename(preset, name), cancellationToken);
    }

    public Task<Preset> DuplicateAsync(Preset preset, string name, CancellationToken cancellationToken = default)
    {
        return SaveAsync(PresetFactory.Duplicate(preset, name), cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetPath(id);
        LocalDataFile.DeleteIfExists(path);
        return Task.CompletedTask;
    }

    private void EnsurePresetsDirectory() => Directory.CreateDirectory(presetsDirectory);

    private string GetPath(Guid id) => Path.Combine(
        presetsDirectory,
        $"{PresetIds.RequireValid(id, nameof(id)):N}.json");

    private static async Task<Preset?> LoadFromPathAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var preset = await LocalJsonFile.ReadRecoverableAsync(
                path,
                WallerJsonContext.Default.Preset,
                cancellationToken);
            return PresetFilePolicy.NormalizeLoaded(preset);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
