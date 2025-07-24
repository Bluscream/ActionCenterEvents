using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

public class EventDirectoryManager
{
    private const string eventsDirName = "Events";
    private List<DirectoryInfo> Roots { get; } = new() {
        new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) ?? string.Empty),
        new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? string.Empty)
    };

    public EventDirectoryManager() { }	
    public EventDirectoryManager(List<DirectoryInfo> roots) { Roots = roots; }

    public void ExecuteEvent(string name, IDictionary<string, string> environmentVariables = null, IEnumerable<string> commandLineArgs = null)
    {
        foreach (var root in Roots)
        {
            var eventDir = root.Combine(eventsDirName, name ?? string.Empty);
            try {
                if (!eventDir.Exists) eventDir.Create();
                foreach (var file in eventDir.Exists ? eventDir.GetFiles("*.*", SearchOption.TopDirectoryOnly) : Array.Empty<FileInfo>())
            {
                ExecuteFile(file.FullName, environmentVariables, commandLineArgs);
            }
            } catch (Exception ex) {
                Utils.Log($"Error executing event {name}: {ex.Message}");
            }
        }
    }

    private void ExecuteFile(string filePath, IDictionary<string, string> environmentVariables = null, IEnumerable<string> commandLineArgs = null)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        try
        {
            var isBatch = filePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
            var isShortcut = filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

            var startInfo = new ProcessStartInfo
            {
                FileName = isBatch ? "cmd.exe" : filePath,
                Arguments = isBatch ? $"/c \"{filePath}\"" : "",
                UseShellExecute = isShortcut ? true : false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (commandLineArgs != null) {
                startInfo.Arguments += string.Join(" ", commandLineArgs.Select(a => $"\"{a}\""));
            }

            if (environmentVariables != null)
            {
                if (!isShortcut)
                {
                    foreach (var kvp in environmentVariables)
                    {
                        Utils.Log($"Setting environment variable {kvp.Key} to {kvp.Value}");
                        startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                    }
                } else {
                    Utils.Log($"Warning: Environment variables are not supported for shortcuts (.lnk files)!");
                }
            }

            Utils.Log($"Executing {filePath} with arguments: {startInfo.Arguments}");

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.EnableRaisingEvents = false;
            }
        }
        catch (Exception ex)
        {
            Utils.Log($"Failed to execute {filePath}: {ex.Message}");
        }
    }
} 