namespace Waller.Native.App.ViewModels;

public sealed record OptionItem<T>(T Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}

internal static class OptionItems
{
    public static void Replace<T>(
        ICollection<OptionItem<T>> target,
        IEnumerable<OptionItem<T>> options)
    {
        target.Clear();
        foreach (var option in options)
        {
            target.Add(option);
        }
    }

    public static OptionItem<T>? Select<T>(
        IEnumerable<OptionItem<T>> options,
        T value,
        IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        return options.FirstOrDefault(option => comparer.Equals(option.Value, value));
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
