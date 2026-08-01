using Waller.Native.Core.Models;
using Waller.Native.Workflows.Presets;

namespace Waller.Native.App.ViewModels;

public sealed partial class PresetsViewModel
{
    internal async Task RefreshAsync(Guid? selectPresetId)
    {
        selectionVersion++;
        isChangingSelection = true;
        try
        {
            var presets = await workflow.ListAsync();
            PresetMenuLists.ReplaceMain(Items, presets, Text.CurrentSetup);
            var normalizedSelection = PresetIds.NormalizeOptional(selectPresetId);
            var selected = PresetMenuLists.Select(Items, normalizedSelection)
                ?? throw new InvalidOperationException("Preset menu refresh did not produce a selection.");
            var requestedPresetMissing = normalizedSelection is not null && selected.Id != normalizedSelection;
            SelectedPreset = selected;
            LastSelectedPresetId = requestedPresetMissing ? null : selected.Id;
            if (requestedPresetMissing)
            {
                await PersistLastSelectedPresetAsync(null);
            }
        }
        finally
        {
            isChangingSelection = false;
        }
    }

    private async Task LoadSelectedPresetAsync(PresetMenuItem item, int loadVersion)
    {
        PresetOperationResult<PresetSelection> result;
        try
        {
            result = await workflow.SelectAsync(activeSession, item.Id);
        }
        catch (Exception)
        {
            if (loadVersion == selectionVersion)
            {
                SetStatus(presetText.LoadFailed);
            }

            return;
        }

        if (loadVersion != selectionVersion)
        {
            return;
        }

        if (!result.TryGetValue(out var selection))
        {
            SetStatus(result.Status == PresetOperationStatus.Missing
                ? presetText.NotFound(item.Name)
                : shellText.LocalDataWriteFailed);
            await RefreshAsync(selectPresetId: null);
            return;
        }

        selectedPresetRecord = selection.SelectedPreset;
        LastSelectedPresetId = selection.SelectedPreset?.Id;
        workspace.ReplaceActiveSession(selection.Session);
        PresetNameDraft = selection.SelectedPreset?.Name ?? string.Empty;
        refreshSessionSurface(!selection.IsCurrentSetup);
        await PersistLastSelectedPresetAsync(LastSelectedPresetId);
        SetStatus(selection.IsCurrentSetup
            ? presetText.CurrentSetupSelected
            : presetText.Loaded(selection.SelectedPreset!.Name));
    }

    private async Task PersistLastSelectedPresetAsync(Guid? presetId)
    {
        var result = await userSettings.UpdateLastSelectedPresetAsync(presetId);
        LastSelectedPresetId = result.TryGetUpdatedSettings(out var updated)
            ? updated.LastSelectedPresetId
            : null;
    }

    private async Task RefreshManagedPresetListAsync(Guid? selectPresetId)
    {
        var presets = await workflow.ListAsync();
        PresetMenuLists.ReplaceManage(ManagedItems, presets);
        SelectedManagedPreset = PresetMenuLists.Select(ManagedItems, selectPresetId);
        OnPropertyChanged(nameof(ManagedPresetEmptyVisibility));
    }

    private async Task<bool> PresentFailureAsync(PresetOperationStatus status)
    {
        if (status == PresetOperationStatus.Missing)
        {
            SetStatus(presetText.MissingPreset);
            await RefreshAsync(activeSession.BasedOnPreset?.Id);
            await RefreshManagedPresetListAsync(selectPresetId: null);
            return true;
        }

        if (status == PresetOperationStatus.WriteFailed)
        {
            SetStatus(shellText.LocalDataWriteFailed);
            return true;
        }

        return false;
    }
}
