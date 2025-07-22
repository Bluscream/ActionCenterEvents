using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Linq;

    public class Config
    {
        public string EnvironmentVariablePrefix { get; set; } = "NOTIFICATION_";
        public bool Console { get; set; } = false;
        public bool csv { get; set; } = true;

    public static Config Load(string[] args, string exePath)
    {
        var exeName = Path.GetFileNameWithoutExtension(exePath);
        if (string.IsNullOrEmpty(exeName) || exeName == ".")
        {
            exeName = "ActionCenterEvents";
        }
        var exeDir = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var programConfigPath = Path.Combine(exeDir, exeName + ".json");
        var userConfigPath = Path.Combine(userDir, exeName + ".json");

        var config = new Config();
        
        if (!File.Exists(programConfigPath))
            config.SaveToFile(programConfigPath);
        else
            config.LoadFromFile(programConfigPath);
        if (!File.Exists(userConfigPath))
            config.SaveToFile(userConfigPath);
        else
            config.LoadFromFile(userConfigPath);

        config.ParseCommandLine(args);
        return config;
    }

    public void LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Config>(json);
            if (loaded == null) return;
            foreach (var prop in typeof(Config).GetProperties())
            {
                var value = prop.GetValue(loaded);
                if (value != null) prop.SetValue(this, value);
            }
        }
        catch { /* ignore errors, fallback to defaults */ }
    }

    public void SaveToFile(string path)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, json);
        }
        catch { /* ignore errors */ }
    }

    public void ParseCommandLine(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if ((arg.StartsWith("--") || arg.StartsWith("-") || arg.StartsWith("/")) && arg.Length > 1)
            {
                var key = arg.TrimStart('-', '/').ToLowerInvariant();
                string? value = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && !args[i + 1].StartsWith("/"))
                {
                    value = args[i + 1];
                    i++;
                }
                switch (key)
                {
                    case "envprefix":
                    case "environmentvariableprefix":
                        EnvironmentVariablePrefix = value ?? new Config().EnvironmentVariablePrefix;
                        break;
                    case "console":
                        // For boolean flags: if no value provided, default to true
                        // If value is provided, parse it as boolean
                        if (value == null)
                        {
                            Console = true;
                        }
                        else
                        {
                            Console = value.ToLowerInvariant() == "true" || value.ToLowerInvariant() == "1";
                        }
                        break;
                    case "csvlogging":
                        // For boolean flags: if no value provided, default to true
                        // If value is provided, parse it as boolean
                        if (value == null)
                        {
                            csv = true;
                        }
                        else
                        {
                            csv = value.ToLowerInvariant() == "true" || value.ToLowerInvariant() == "1";
                        }
                        break;
                }
            }
        }
    }


}