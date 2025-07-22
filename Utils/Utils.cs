using System;
using System.Runtime.InteropServices;
using System.Threading;

static class Utils
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AllocConsole();

    private static bool _consoleEnabled = false;

    internal static void CreateConsole()
    {
        AllocConsole();
        _consoleEnabled = true;
    }

    internal static void SetConsoleTitle(string title)
    {
        Console.Title = title;
    }

    internal static void SetConsoleEnabled(bool enabled)
    {
        _consoleEnabled = enabled;
    }

    internal static void Log(string message)
    {
        if (_consoleEnabled)
        {
            Console.WriteLine(message);
        }
    }

    public static T RunInSTA<T>(Func<T> func)
    {
        T? result = default;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try { result = func(); }
            catch (Exception ex) { exception = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null) throw exception;
        return result!;
    }
}