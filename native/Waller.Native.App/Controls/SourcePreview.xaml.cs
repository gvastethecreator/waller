using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Waller.Native.App.Controls;

public sealed partial class SourcePreview : UserControl
{
    public static readonly DependencyProperty PreviewWidthProperty = DependencyProperty.Register(
        nameof(PreviewWidth),
        typeof(double),
        typeof(SourcePreview),
        new PropertyMetadata(112d));

    public static readonly DependencyProperty PreviewHeightProperty = DependencyProperty.Register(
        nameof(PreviewHeight),
        typeof(double),
        typeof(SourcePreview),
        new PropertyMetadata(64d));

    public static readonly DependencyProperty PreviewBrushProperty = DependencyProperty.Register(
        nameof(PreviewBrush),
        typeof(Brush),
        typeof(SourcePreview),
        new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewImageBrushProperty = DependencyProperty.Register(
        nameof(PreviewImageBrush),
        typeof(Brush),
        typeof(SourcePreview),
        new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewImageVisibilityProperty = DependencyProperty.Register(
        nameof(PreviewImageVisibility),
        typeof(Visibility),
        typeof(SourcePreview),
        new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty PreviewTextProperty = DependencyProperty.Register(
        nameof(PreviewText),
        typeof(string),
        typeof(SourcePreview),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PreviewTextVisibilityProperty = DependencyProperty.Register(
        nameof(PreviewTextVisibility),
        typeof(Visibility),
        typeof(SourcePreview),
        new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty PreviewTextFontSizeProperty = DependencyProperty.Register(
        nameof(PreviewTextFontSize),
        typeof(double),
        typeof(SourcePreview),
        new PropertyMetadata(14d));

    public static readonly DependencyProperty PreviewTextTrimmingProperty = DependencyProperty.Register(
        nameof(PreviewTextTrimming),
        typeof(TextTrimming),
        typeof(SourcePreview),
        new PropertyMetadata(TextTrimming.None));

    public static readonly DependencyProperty PreviewTextWrappingProperty = DependencyProperty.Register(
        nameof(PreviewTextWrapping),
        typeof(TextWrapping),
        typeof(SourcePreview),
        new PropertyMetadata(TextWrapping.Wrap));

    public static readonly DependencyProperty PreviewBadgeTextProperty = DependencyProperty.Register(
        nameof(PreviewBadgeText),
        typeof(string),
        typeof(SourcePreview),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PreviewBadgeVisibilityProperty = DependencyProperty.Register(
        nameof(PreviewBadgeVisibility),
        typeof(Visibility),
        typeof(SourcePreview),
        new PropertyMetadata(Visibility.Collapsed));

    public SourcePreview()
    {
        InitializeComponent();
    }

    public double PreviewWidth
    {
        get => (double)GetValue(PreviewWidthProperty);
        set => SetValue(PreviewWidthProperty, value);
    }

    public double PreviewHeight
    {
        get => (double)GetValue(PreviewHeightProperty);
        set => SetValue(PreviewHeightProperty, value);
    }

    public Brush? PreviewBrush
    {
        get => (Brush?)GetValue(PreviewBrushProperty);
        set => SetValue(PreviewBrushProperty, value);
    }

    public Brush? PreviewImageBrush
    {
        get => (Brush?)GetValue(PreviewImageBrushProperty);
        set => SetValue(PreviewImageBrushProperty, value);
    }

    public Visibility PreviewImageVisibility
    {
        get => (Visibility)GetValue(PreviewImageVisibilityProperty);
        set => SetValue(PreviewImageVisibilityProperty, value);
    }

    public string PreviewText
    {
        get => (string)GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    public Visibility PreviewTextVisibility
    {
        get => (Visibility)GetValue(PreviewTextVisibilityProperty);
        set => SetValue(PreviewTextVisibilityProperty, value);
    }

    public double PreviewTextFontSize
    {
        get => (double)GetValue(PreviewTextFontSizeProperty);
        set => SetValue(PreviewTextFontSizeProperty, value);
    }

    public TextTrimming PreviewTextTrimming
    {
        get => (TextTrimming)GetValue(PreviewTextTrimmingProperty);
        set => SetValue(PreviewTextTrimmingProperty, value);
    }

    public TextWrapping PreviewTextWrapping
    {
        get => (TextWrapping)GetValue(PreviewTextWrappingProperty);
        set => SetValue(PreviewTextWrappingProperty, value);
    }

    public string PreviewBadgeText
    {
        get => (string)GetValue(PreviewBadgeTextProperty);
        set => SetValue(PreviewBadgeTextProperty, value);
    }

    public Visibility PreviewBadgeVisibility
    {
        get => (Visibility)GetValue(PreviewBadgeVisibilityProperty);
        set => SetValue(PreviewBadgeVisibilityProperty, value);
    }
}
