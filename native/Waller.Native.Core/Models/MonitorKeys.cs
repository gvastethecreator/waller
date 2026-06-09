namespace Waller.Native.Core.Models;

public static class MonitorKeys
{
    public static StringComparer Comparer { get; } = StringComparer.OrdinalIgnoreCase;

    public static bool Equals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static HashSet<string> CreateSet()
    {
        return new HashSet<string>(Comparer);
    }

    public static HashSet<string> CreateSet(string monitorKey)
    {
        return new HashSet<string>(Comparer) { monitorKey };
    }

    public static HashSet<string> CreateSet(IEnumerable<string> monitorKeys)
    {
        return new HashSet<string>(monitorKeys, Comparer);
    }
}
