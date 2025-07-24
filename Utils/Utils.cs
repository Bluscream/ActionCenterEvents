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

    internal static void Log(string message)
    {
        if (_consoleEnabled)
        {
            Console.WriteLine(message);
        }
    }

    public static string GetOwnPath() {
        var possiblePaths = new List<string?> {
            Process.GetCurrentProcess().MainModule?.FileName,
            AppContext.BaseDirectory,
            Environment.GetCommandLineArgs().FirstOrDefault(),
            // Assembly.GetEntryAssembly()?.Location, // Removed due to IL3000 warning
            ".",
        };
        foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p))) {
            if (System.IO.File.Exists(path!)) {
                return System.IO.Path.GetFullPath(path!);
            }
        }
        return null;
    }
}