using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record SettingsPreferenceSaveResult
{
    public SettingsPreferenceSaveResult(
        Guid? LastSelectedPresetId,
        bool WriteFailed)
    {
        var normalizedLastSelectedPresetId = PresetIds.NormalizeOptional(LastSelectedPresetId);
        if (WriteFailed && normalizedLastSelectedPresetId is not null)
        {
            throw new ArgumentException("Failed Settings save results cannot include last selected Preset.", nameof(LastSelectedPresetId));
        }

        this.LastSelectedPresetId = normalizedLastSelectedPresetId;
        this.WriteFailed = WriteFailed;
    }

    public Guid? LastSelectedPresetId { get; }

    public bool WriteFailed { get; }

    public static SettingsPreferenceSaveResult Success(Guid? lastSelectedPresetId) =>
        new(lastSelectedPresetId, WriteFailed: false);

    public static SettingsPreferenceSaveResult LocalWriteFailed() =>
        new(LastSelectedPresetId: null, WriteFailed: true);

    public string StatusText(ShellStatusTextPresenter shellText)
    {
        ArgumentNullException.ThrowIfNull(shellText);

        return WriteFailed
            ? shellText.LocalDataWriteFailed
            : shellText.SettingsSaved;
    }

    public bool TryGetSavedLastSelectedPresetId(out Guid? lastSelectedPresetId)
    {
        lastSelectedPresetId = LastSelectedPresetId;
        return !WriteFailed;
    }
}
