using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

public class EventDirectoryManager
{
    public DirectoryInfo GlobalRoot { get; }
    public DirectoryInfo UserRoot { get; }

    public EventDirectoryManager()
    {
        GlobalRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Events"));
        UserRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Events"));
    }

    public DirectoryInfo GetEventSubdirectory(string subDir, bool global)
    {
        var root = global ? GlobalRoot : UserRoot;
        return new DirectoryInfo(Path.Combine(root.FullName, subDir));
    }

    public IEnumerable<DirectoryInfo> GetAllEventSubdirectories(string subDir)
    {
        yield return GetEventSubdirectory(subDir, true);
        yield return GetEventSubdirectory(subDir, false);
    }

    public void EnsureEventSubdirectoriesExist(string subDir)
    {
        foreach (var dir in GetAllEventSubdirectories(subDir))
        {
            if (!dir.Exists)
            {
                dir.Create();
            }
        }
    }

    public IEnumerable<FileInfo> GetFilesInEventSubdirectory(string subDir, string searchPattern = "*.*")
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

    public void ExecuteEventDirectories(Config config, string subDir, string appId, string toastTitle, string toastBody, string payload, string timestamp, List<string> images)
    {
        EnsureEventSubdirectoriesExist(subDir);
        foreach (var file in GetFilesInEventSubdirectory(subDir))
        {
            ExecuteFile(config, file.FullName, appId, toastTitle, toastBody, payload, timestamp, images);
        }
    }

    private void ExecuteFile(Config config, string filePath, string appId, string toastTitle, string toastBody, string payload, string timestamp, List<string> images)
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
                CreateNoWindow = true, // Hide window for non-shortcuts
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!isShortcut)
            {
                // Set environment variables with configurable prefix
                var prefix = config.EnvironmentVariablePrefix;
                startInfo.EnvironmentVariables[prefix + "APPID"] = appId;
                startInfo.EnvironmentVariables[prefix + "TITLE"] = toastTitle;
                startInfo.EnvironmentVariables[prefix + "BODY"] = toastBody;
                startInfo.EnvironmentVariables[prefix + "PAYLOAD"] = payload;
                startInfo.EnvironmentVariables[prefix + "TIMESTAMP"] = timestamp;
                startInfo.EnvironmentVariables[prefix + "DATETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                for (int i = 0; i < images.Count; i++)
                {
                    var varName = prefix + $"IMAGE{i + 1}";
                    Utils.Log($"Setting environment variable {varName} to {images[i]}");
                    startInfo.EnvironmentVariables[varName] = images[i];
                }
            }
            else
            {
                Utils.Log($"Warning: Environment variables are not supported for shortcuts (.lnk files)!");
            }

            Utils.Log($"Executing {filePath} with arguments: {startInfo.Arguments}");

            var process = Process.Start(startInfo);
            // Do not wait for exit, do not read output
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