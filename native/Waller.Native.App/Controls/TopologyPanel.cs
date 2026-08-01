using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;
using Windows.Foundation;

namespace Waller.Native.App.Controls;

/// <summary>
/// Arranges topology item containers at the coordinates calculated from the
/// Windows virtual desktop. ItemsControl creates ContentPresenters for each
/// monitor, so the panel owns the absolute arrange step instead of relying on
/// a StackPanel that would erase the real monitor positions.
/// </summary>
public sealed class TopologyPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var measuredWidth = 0d;
        var measuredHeight = 0d;

        foreach (var child in Children)
        {
            child.Measure(availableSize);

            if (TryGetMonitor(child, out var monitor))
            {
                measuredWidth = Math.Max(measuredWidth, monitor.TopologyLeft + monitor.TopologyWidth);
                measuredHeight = Math.Max(measuredHeight, monitor.TopologyTop + monitor.TopologyHeight);
            }
            else
            {
                measuredWidth = Math.Max(measuredWidth, child.DesiredSize.Width);
                measuredHeight = Math.Max(measuredHeight, child.DesiredSize.Height);
            }
        }

        return new Size(
            double.IsFinite(availableSize.Width) ? availableSize.Width : measuredWidth,
            double.IsFinite(availableSize.Height) ? availableSize.Height : measuredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            if (TryGetMonitor(child, out var monitor))
            {
                child.Arrange(new Rect(
                    monitor.TopologyLeft,
                    monitor.TopologyTop,
                    monitor.TopologyWidth,
                    monitor.TopologyHeight));
            }
            else
            {
                child.Arrange(new Rect(0, 0, 0, 0));
            }
        }

        return finalSize;
    }

    private static bool TryGetMonitor(UIElement child, out MonitorRowViewModel monitor)
    {
        monitor = null!;

        if (child is ContentPresenter { Content: MonitorRowViewModel content })
        {
            monitor = content;
            return true;
        }

        if (child is FrameworkElement { DataContext: MonitorRowViewModel dataContext })
        {
            monitor = dataContext;
            return true;
        }

        return false;
    }
}
