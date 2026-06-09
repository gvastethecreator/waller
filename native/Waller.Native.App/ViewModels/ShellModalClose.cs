namespace Waller.Native.App.ViewModels;

internal static class ShellModalClose
{
    public static void Dispatch(
        ShellModalLayer layer,
        Action closeDeleteConfirmation,
        Action closeManagePresets,
        Action closeSaveAs,
        Action closeSettings)
    {
        switch (layer)
        {
            case ShellModalLayer.DeleteConfirmation:
                closeDeleteConfirmation();
                break;
            case ShellModalLayer.ManagePresets:
                closeManagePresets();
                break;
            case ShellModalLayer.SaveAs:
                closeSaveAs();
                break;
            case ShellModalLayer.Settings:
                closeSettings();
                break;
        }
    }
}
