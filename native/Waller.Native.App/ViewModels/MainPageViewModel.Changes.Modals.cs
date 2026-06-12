namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnIsManagePresetsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ManagePresetsModalSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsSaveAsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SaveAsModalSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsDeleteConfirmationOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.DeleteConfirmationSurface);
        NotifyModalStateChanged();
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SettingsModalSurface);
        NotifyModalStateChanged();
    }
}
