namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnIsApplyingChanged(bool value)
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.ApplySurface);
        NotifyCommandStateChanged();
    }
}
