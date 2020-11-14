using System;
using System.Diagnostics;
using System.IO;
using AppKit;
using SimpleJSON;

public class MacSpecific : IPlatformSpecific
{
    private NSTextField progressLabel;
    private NSProgressIndicator progressBar;

    public MacSpecific(NSTextField progressLabel, NSProgressIndicator progressBar)
    {
        this.progressLabel = progressLabel;
        this.progressBar = progressBar;
    }

    public IVersion GetVersion()
    {
        return OSX.Version.GetVersion();
    }

    public JSONNode GetCMConfig()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string cmSettingsPath = Path.Combine(homeDir, "Library", "Application Support", "com.BinaryElement.ChroMapper", "ChroMapperSettings.json");

        using (StreamReader reader = new StreamReader(cmSettingsPath))
        {
            return JSON.Parse(reader.ReadToEnd());
        }
    }

    public string GetCDNPrefix()
    {
        return "osx/";
    }

    public void UpdateLabel(string label)
    {
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            progressLabel.StringValue = label;
        });
    }

    public void UpdateProgress(float progress)
    {
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            progressBar.Indeterminate = false;
            progressBar.DoubleValue = progress * 100;
        });
    }

    public void Exit()
    {
        var startInfo = new ProcessStartInfo()
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"chmod +x /Applications/ChroMapper.app/Contents/MacOS/ChroMapper\"",

            CreateNoWindow = true
        };
        Process.Start(startInfo).WaitForExit();

        var startInfo2 = new ProcessStartInfo("/Applications/ChroMapper.app/Contents/MacOS/ChroMapper")
        {
            WorkingDirectory = "/Applications",
            Arguments = $"--launcher \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName)}\""
        };

        Process.Start(startInfo2);

        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            NSApplication.SharedApplication.Terminate(NSApplication.SharedApplication);
        });
    }

    public string GetJenkinsFilename()
    {
        return "build/ChroMapper-MacOS.tar.gz";
    }

    public string GetCDNFilename()
    {
        return "MacOS.tar.gz";
    }

    public string GetDownloadFolder()
    {
        return "/Applications";
    }

    public string LocalFolderName()
    {
        return "";
    }
}
