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
        return new HashSet<string>(Comparer) { Require(monitorKey, nameof(monitorKey)) };
    }

    public static HashSet<string> CreateSet(IEnumerable<string> monitorKeys)
    {
        ArgumentNullException.ThrowIfNull(monitorKeys);

        var set = CreateSet();
        foreach (var monitorKey in monitorKeys)
        {
            set.Add(Require(monitorKey, nameof(monitorKeys)));
        }

        return set;
    }

    public static bool Contains(IReadOnlySet<string> monitorKeys, string monitorKey)
    {
        ArgumentNullException.ThrowIfNull(monitorKeys);
        monitorKey = Require(monitorKey, nameof(monitorKey));

        return monitorKeys.Any(candidate => Equals(candidate, monitorKey));
    }

    public static string Require(string monitorKey, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(monitorKey))
        {
            throw new ArgumentException("Monitor key is required.", parameterName);
        }

        return monitorKey;
    }
}
