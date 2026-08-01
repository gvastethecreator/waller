using Waller.Native.Core.Settings;
using Waller.Native.Workflows.Settings;

namespace Waller.Native.Workflows.Windowing;

public sealed class WindowPlacementWorkflow
{
    private readonly UserSettingsWorkflow userSettings;

    public WindowPlacementWorkflow(UserSettingsWorkflow userSettings)
    {
        ArgumentNullException.ThrowIfNull(userSettings);
        this.userSettings = userSettings;
    }

    public async Task<WindowPlacement> RestoreAsync(
        WindowWorkArea workArea,
        CancellationToken cancellationToken = default)
    {
        var settings = await userSettings.LoadAsync(cancellationToken).ConfigureAwait(false);
        return WindowPlacementPolicy.Resolve(
            settings,
            workArea.X,
            workArea.Y,
            workArea.Width,
            workArea.Height);
    }

    public Task<UserSettingsUpdateResult> SaveAsync(
        WindowPlacement placement,
        CancellationToken cancellationToken = default)
    {
        if (placement.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), "Window width must be positive.");
        }

        if (placement.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement), "Window height must be positive.");
        }

        return userSettings.UpdateWindowPlacementAsync(
            placement.Width,
            placement.Height,
            placement.X,
            placement.Y,
            cancellationToken);
    }
}
