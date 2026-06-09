using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial class MissingMonitorRowViewModel(PresetAssignment assignment, LocalizedText text) : ObservableObject
{
    private LocalizedText text = text;

    public PresetAssignment Assignment { get; } = assignment;

    public string DisplayName =>
        Assignment.SavedMonitor.DeviceName
        ?? Assignment.SavedMonitor.MonitorKey;

    public string Resolution => text.Resolution(Assignment.SavedMonitor.Width, Assignment.SavedMonitor.Height);

    public string Bounds => text.Bounds(Assignment.SavedMonitor.X, Assignment.SavedMonitor.Y);

    public bool IsMissingImageSource =>
        WallpaperSourceFiles.IsMissingImageFile(Assignment.Source);

    public string SourceSummary => MonitorSourceText.Summary(Assignment.Source, text);

    public Brush SourcePreviewBrush =>
        MonitorSourcePreview.BaseBrush(Assignment.Source);

    public Brush? SourcePreviewImageBrush =>
        MonitorSourcePreview.ImageBrush(Assignment.Source, Assignment.Placement);

    public bool HasSourcePreviewImage =>
        WallpaperSourceFiles.HasExistingImageFile(Assignment.Source);

    public Visibility SourcePreviewImageVisibility =>
        VisibilityStates.When(HasSourcePreviewImage);

    public Visibility SourcePreviewTextVisibility =>
        VisibilityStates.Unless(HasSourcePreviewImage);

    public string PlacementSummary => text.PlacementSummary(Assignment.Placement);

    public void ReplaceText(LocalizedText text)
    {
        this.text = text;
        NotifyPropertiesChanged(MonitorRowNotificationGroups.MissingMonitorText);
    }

    private void NotifyPropertiesChanged(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
