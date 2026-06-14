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
            case ShellModalLayer.None:
                break;
            case ShellModalLayer.DeleteConfirmation:
                Invoke(closeDeleteConfirmation, nameof(closeDeleteConfirmation));
                break;
            case ShellModalLayer.ManagePresets:
                Invoke(closeManagePresets, nameof(closeManagePresets));
                break;
            case ShellModalLayer.SaveAs:
                Invoke(closeSaveAs, nameof(closeSaveAs));
                break;
            case ShellModalLayer.Settings:
                Invoke(closeSettings, nameof(closeSettings));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer),
                    layer,
                    "Unknown shell modal layer.");
        }
    }

    private static void Invoke(Action action, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(action, parameterName);
        action();
    }
}
