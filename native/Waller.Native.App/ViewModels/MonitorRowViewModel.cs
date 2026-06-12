using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial class MonitorRowViewModel(
    MonitorSession session,
    LocalizedText text,
    double topologyLeft = 0,
    double topologyTop = 0,
    double topologyWidth = 96,
    double topologyHeight = 54) : ObservableObject
{
    private LocalizedText text = text;

    [ObservableProperty]
    public partial MonitorSession Session { get; set; } = session;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TopologyOpacity))]
    [NotifyPropertyChangedFor(nameof(TopologyBorderThickness))]
    public partial bool IsSelected { get; set; }

    public string MonitorKey => Session.Monitor.Identity.MonitorKey;

    public string DisplayName => Session.Monitor.DisplayName;

    public string Resolution => text.Resolution(Session.Monitor.Bounds.Width, Session.Monitor.Bounds.Height);

    public string Bounds => text.Bounds(Session.Monitor.Bounds.X, Session.Monitor.Bounds.Y);

    public double TopologyLeft { get; } = topologyLeft;

    public double TopologyTop { get; } = topologyTop;

    public double TopologyWidth { get; } = topologyWidth;

    public double TopologyHeight { get; } = topologyHeight;

    public double TopologyOpacity => IsSelected ? 1.0 : 0.62;

    public Thickness TopologyBorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    public Visibility TopologyResolutionVisibility =>
        VisibilityStates.When(TopologyWidth >= 92 && TopologyHeight >= 48);

    public string SourceSummary =>
        MonitorSourceText.Summary(Session.DesiredAssignment.Source, text);

    public Brush SourcePreviewBrush =>
        MonitorSourcePreview.BaseBrush(Session.DesiredAssignment.Source);

    public Brush? SourcePreviewImageBrush
        => MonitorSourcePreview.ImageBrush(
            Session.DesiredAssignment.Source,
            Session.DesiredAssignment.Placement);

    public bool HasSourcePreviewImage =>
        WallpaperSourceFiles.HasExistingImageFile(Session.DesiredAssignment.Source);

    public Visibility SourcePreviewImageVisibility =>
        VisibilityStates.When(HasSourcePreviewImage);

    public Visibility SourcePreviewTextVisibility =>
        VisibilityStates.Unless(HasSourcePreviewImage);

    public bool IsMissingImageSource =>
        WallpaperSourceFiles.IsMissingImageFile(Session.DesiredAssignment.Source);

    public string PlacementSummary => text.PlacementSummary(Session.DesiredAssignment.Placement);

    public string StatusSummary => text.MonitorStatusSummary(
        Session.ApplyStatus,
        Session.ApplyError,
        IsMissingImageSource,
        Session.HasUnsavedPresetChanges);

    public string TopologyAccessibleName =>
        $"{DisplayName}, {Resolution}, {Bounds}, {PlacementSummary}, {StatusSummary}";

    public string EditAccessibleName => $"{text.Edit} {DisplayName}";

    public string ApplyAccessibleName => $"{text.Apply} {DisplayName}";

    public void ReplaceText(LocalizedText text)
    {
        this.text = text;
        NotifyPropertiesChanged(MonitorRowNotificationGroups.CurrentMonitorText);
    }

    public void ReplaceSession(MonitorSession session)
    {
        Session = session;
        NotifyPropertiesChanged(MonitorRowNotificationGroups.CurrentMonitorSession);
    }

    private void NotifyPropertiesChanged(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
