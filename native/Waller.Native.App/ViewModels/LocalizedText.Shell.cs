using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public string RenderedCacheClearSummary(RenderedCacheClearResult result) =>
        !result.HasFailures
            ? Format(RenderedCacheClearedFormat, result.Deleted)
            : Format(RenderedCachePartiallyClearedFormat, result.Deleted, result.Failed);

    public string SessionSummary(
        PresetIdentity? basedOnPreset,
        bool hasUnsavedPresetChanges,
        int missingAssignmentCount,
        PresetMenuItem? selectedPreset)
    {
        var name = basedOnPreset?.Name ?? CurrentSetup;
        var modified = hasUnsavedPresetChanges ? $" - {ModifiedSuffix}" : string.Empty;
        var missing = missingAssignmentCount > 0
            ? $" - {missingAssignmentCount} {DisconnectedSuffix}"
            : string.Empty;
        var visualOnly = basedOnPreset is null && selectedPreset?.Id is not null
            ? $" - {selectedPreset.Name} {VisualOnlySuffix}"
            : string.Empty;

        return $"{name}{modified}{missing}{visualOnly}";
    }
}
