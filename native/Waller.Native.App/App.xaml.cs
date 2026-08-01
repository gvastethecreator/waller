using Microsoft.UI.Xaml;
using Waller.Native.App.Platform;

namespace Waller.Native.App;

public partial class App : Application
{
    private Task<WallerAppComposition>? compositionTask;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        compositionTask ??= WallerAppComposition.CreateAsync();
        var composition = await compositionTask;
        composition.Window.Activate();
    }
}
