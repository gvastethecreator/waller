namespace Waller.Native.Core.Models;

public static class DefinedEnumValue
{
    public static bool IsDefined<T>(T value)
        where T : struct, Enum =>
        Enum.IsDefined(value);

    public static T Require<T>(T value, string parameterName, string message)
        where T : struct, Enum
    {
        if (!IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }

        return value;
    }
}
