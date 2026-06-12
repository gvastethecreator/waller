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
        return new HashSet<string>(Comparer) { RequiredMonitorKey(monitorKey, nameof(monitorKey)) };
    }

    public static HashSet<string> CreateSet(IEnumerable<string> monitorKeys)
    {
        ArgumentNullException.ThrowIfNull(monitorKeys);

        var set = CreateSet();
        foreach (var monitorKey in monitorKeys)
        {
            set.Add(RequiredMonitorKey(monitorKey, nameof(monitorKeys)));
        }

        return set;
    }

    private static string RequiredMonitorKey(string monitorKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(monitorKey))
        {
            throw new ArgumentException("Monitor key is required.", parameterName);
        }

        return monitorKey;
    }
}
