using System.Runtime.ExceptionServices;

namespace Waller.Native.Core.Windows;

internal static class StaThreadRunner
{
    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                captured = error;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
