namespace Waller.Native.App.ViewModels;

public sealed record OptionItem<T>
{
    public OptionItem(T Value, string DisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(DisplayName);

        this.Value = Value;
        this.DisplayName = DisplayName;
    }

    public T Value { get; }

    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}

internal static class OptionItems
{
    public static void Replace<T>(
        ICollection<OptionItem<T>> target,
        IEnumerable<OptionItem<T>> options)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(options);

        target.Clear();
        foreach (var option in options)
        {
            target.Add(option ?? throw new ArgumentException("Option collection cannot include null items.", nameof(options)));
        }
    }

    public static OptionItem<T>? Select<T>(
        IEnumerable<OptionItem<T>> options,
        T value,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        comparer ??= EqualityComparer<T>.Default;
        return options.FirstOrDefault(option =>
        {
            ArgumentNullException.ThrowIfNull(option);
            return comparer.Equals(option.Value, value);
        });
    }

    public static OptionItem<T>? ReplaceAndSelect<T>(
        ICollection<OptionItem<T>> target,
        IEnumerable<OptionItem<T>> options,
        T value,
        IEqualityComparer<T>? comparer = null)
    {
        Replace(target, options);
        return Select(target, value, comparer);
    }
}
