﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Threading;
using SimpleJSON;

public class LinuxSpecific : IPlatformSpecific
{
    public LinuxSpecific() {}

    public IVersion GetVersion()
    {
        return Version.GetVersion();
    }

    public JSONNode GetCMConfig()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        string cmSettingsPath = Path.Combine(path, "unity3d", "BinaryElement", "ChroMapper", "ChroMapperSettings.json");

        if (!File.Exists(cmSettingsPath))
        {
            return new JSONObject();
        }

        using (StreamReader reader = new StreamReader(cmSettingsPath))
        {
            return JSON.Parse(reader.ReadToEnd());
        }
    }

    public string GetCDNPrefix()
    {
        return "nix/";
    }

    private ProgressBar _progressBar;

    public static void RewriteLine(int lineNumber, String newText)
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, currentLineCursor - lineNumber);
        Console.Write(newText); Console.WriteLine(new string(' ', Console.WindowWidth - newText.Length)); 
        Console.SetCursorPosition(0, currentLineCursor);
    }
    
    public void UpdateLabel(string label)
    {
        if (_progressBar != null)
        {
            _progressBar.Dispose();
            _progressBar = null;
            Console.WriteLine("");
        }
        
        if (label.StartsWith("Extracting") || label.StartsWith("Patching"))
        {
            if (Monitor.TryEnter(this)) {
                try
                {
                    RewriteLine(1, label);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            //Console.WriteLine(label);
            return;
        }

        Console.Write(label);
        _progressBar = new ProgressBar();
    }

    public void UpdateProgress(float progress)
    {
        if (_progressBar != null)
        {
            _progressBar.Report(progress);
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string pathname, int mode);
    
    public void Exit()
    {
        var cmWindowStyle = ProcessWindowStyle.Normal;

        var exeFile = Path.Combine(GetDownloadFolder(), "chromapper", "ChroMapper");
        chmod(exeFile, 0x1 | 0x4 | 0x8 | 0x20 | 0x40 | 0x80 | 0x100);

        var startInfo = new ProcessStartInfo(exeFile)
        {
            WorkingDirectory = Path.Combine(GetDownloadFolder(), "chromapper"),
            Arguments = $"--launcher \"{GetCMLPath()}\"",
            WindowStyle = cmWindowStyle
        };

        Process.Start(startInfo);
        
        Environment.Exit(0);
    }

    public void Restart(string tmpFile)
    {
        // Overwrite us
        var newExe = GetCMLPath();
        var oldExe = GetTempCMLPath();
        File.Move(newExe, oldExe);
        File.Move(tmpFile, newExe);

        // Run us
        var startInfo = new ProcessStartInfo(AppDomain.CurrentDomain.FriendlyName)
        {
            WorkingDirectory = GetDownloadFolder()
        };

        Process.Start(startInfo);

        Environment.Exit(0);
    }

    public void CleanupUpdate()
    {
        try
        {
            var oldExe = GetTempCMLPath();
            if (File.Exists(oldExe))
            {
                File.Delete(oldExe);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public string GetJenkinsFilename()
    {
        return "ChroMapper-Linux.tar.gz";
    }

    public string GetCDNFilename()
    {
        return "Linux.tar.gz";
    }

    public string GetDownloadFolder()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    public string LocalFolderName()
    {
        return "";
    }

    public string GetCMLFilename()
    {
        return "CML-Linux";
    }

    private string GetTempCMLPath()
    {
        return Path.Combine(GetDownloadFolder(), "CML-Linux.old");
    }

    private string GetCMLPath()
    {
        return Path.Combine(GetDownloadFolder(), AppDomain.CurrentDomain.FriendlyName);
    }
}