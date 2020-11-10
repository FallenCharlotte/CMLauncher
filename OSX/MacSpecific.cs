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
        // TODO: Read cm config
        return JSON.Parse("{}");
    }

    public string GetCDNPrefix()
    {
        return "osx/";
    }

    public void UpdateLabel(string label)
    {
        progressLabel.StringValue = label;
    }

    public void UpdateProgress(float progress)
    {
        progressBar.DoubleValue = progress * 100;
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

        NSApplication.SharedApplication.Terminate(NSApplication.SharedApplication);
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
