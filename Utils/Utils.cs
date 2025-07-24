using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    internal static void Log(object message, params object[] args)
    {
        if (!_consoleEnabled) return;
        var msg = message?.ToString() ?? string.Empty;
        #pragma warning disable CS8604
        Console.WriteLine(args != null && args.Length > 0 ? string.Format(msg, args) : msg);
        #pragma warning restore CS8604
    }

    public static string GetOwnPath() {
        var possiblePaths = new List<string?> {
            Process.GetCurrentProcess().MainModule?.FileName,
            AppContext.BaseDirectory,
            Environment.GetCommandLineArgs().FirstOrDefault(),
            #pragma warning disable IL3000
            Assembly.GetEntryAssembly()?.Location,
            #pragma warning restore IL3000
            ".",
        };
        foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p))) {
            if (System.IO.File.Exists(path!)) {
                return System.IO.Path.GetFullPath(path!);
            }
        }
        #pragma warning disable CS8603
        return string.Empty;
        #pragma warning restore CS8603
    }
}