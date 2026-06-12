namespace Waller.Native.Core.Models;

internal static class RequiredList
{
    public static IReadOnlyList<T> Copy<T>(
        IReadOnlyList<T> value,
        string parameterName,
        string nullItemMessage)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        ValidateItems(value, parameterName, nullItemMessage);
        return value.ToList();
    }

    public static void ValidateItems<T>(
        IReadOnlyList<T> value,
        string parameterName,
        string nullItemMessage)
        where T : class
    {
        foreach (var item in value)
        {
            if (item is null)
            {
                throw new ArgumentException(nullItemMessage, parameterName);
            }
        }
    }
}
