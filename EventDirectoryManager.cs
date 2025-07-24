using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

public class EventDirectoryManager
{
    private DirectoryInfo GlobalRoot { get; }
    private DirectoryInfo UserRoot { get; }

    public EventDirectoryManager()
    {
        GlobalRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Events"));
        UserRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Events"));
    }

    public void ExecuteEvent(string eventName, Dictionary<string, string> environmentVariables)
    {
        EnsureEventSubdirectoriesExist(eventName);
        foreach (var file in GetFilesInEventSubdirectory(eventName))
        {
            ExecuteFile(file.FullName, environmentVariables);
        }
    }

    private DirectoryInfo GetEventSubdirectory(string subDir, bool global)
    {
        var root = global ? GlobalRoot : UserRoot;
        return new DirectoryInfo(Path.Combine(root.FullName, subDir));
    }

    private IEnumerable<DirectoryInfo> GetAllEventSubdirectories(string subDir)
    {
        yield return GetEventSubdirectory(subDir, true);
        yield return GetEventSubdirectory(subDir, false);
    }

    private void EnsureEventSubdirectoriesExist(string subDir)
    {
        foreach (var dir in GetAllEventSubdirectories(subDir))
        {
            if (!dir.Exists)
            {
                dir.Create();
            }
        }
    }

    private IEnumerable<FileInfo> GetFilesInEventSubdirectory(string subDir, string searchPattern = "*.*")
    {
        foreach (var dir in GetAllEventSubdirectories(subDir))
        {
            if (dir.Exists)
            {
                foreach (var file in dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly))
                {
                    yield return file;
                }
            }
        }
    }

    private void ExecuteFile(string filePath, Dictionary<string, string> environmentVariables)
    {
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

            if (!isShortcut)
            {
                foreach (var kvp in environmentVariables)
                {
                    Utils.Log($"Setting environment variable {kvp.Key} to {kvp.Value}");
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                Utils.Log($"Warning: Environment variables are not supported for shortcuts (.lnk files)!");
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