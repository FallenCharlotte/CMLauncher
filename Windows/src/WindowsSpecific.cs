﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CM_Launcher;
using SimpleJSON;

public class WindowsSpecific : IPlatformSpecific
{
    private readonly Updater updater;

    public WindowsSpecific(Updater updater)
    {
        this.updater = updater;
    }

    public IVersion GetVersion()
    {
        return Version.GetVersion();
    }

    private string GetKnownFolderPath(Guid knownFolderId)
    {
        IntPtr pszPath = IntPtr.Zero;
        try
        {
            int hr = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out pszPath);
            if (hr >= 0)
                return Marshal.PtrToStringAuto(pszPath);
            throw Marshal.GetExceptionForHR(hr);
        }
        finally
        {
            if (pszPath != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pszPath);
        }
    }

    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

    public JSONNode GetCMConfig()
    {
        Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
        string cmSettingsPath = Path.Combine(GetKnownFolderPath(localLowId), "BinaryElement", "ChroMapper", "ChroMapperSettings.json");

        using (StreamReader reader = new StreamReader(cmSettingsPath))
        {
            return JSON.Parse(reader.ReadToEnd());
        }
    }

    public string GetCDNPrefix()
    {
        return "win/";
    }

    public void UpdateLabel(string label)
    {
        updater.UpdateLabel(label);
    }

    public void UpdateProgress(float progress)
    {
        updater.Report(progress);
    }

    public void Exit()
    {
        var startInfo = new ProcessStartInfo("ChroMapper.exe")
        {
            WorkingDirectory = Path.Combine(GetDownloadFolder(), "chromapper"),
            Arguments = $"--launcher \"{Path.Combine(GetDownloadFolder(), AppDomain.CurrentDomain.FriendlyName)}\""
        };

        Process.Start(startInfo);

        Application.Exit();
    }

    public string GetJenkinsFilename()
    {
        return "ChroMapper-Win64.zip";
    }

    public string GetCDNFilename()
    {
        return "Win64.zip";
    }

    public string GetDownloadFolder()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    public string LocalFolderName()
    {
        return "chromapper";
    }

    public void PerformAuth()
    {
        Process.Start($"{Config.AUTH_URL}/auth");
    }

    public string[] GetAuthTokens()
    {
        var version = Version.GetVersion();
        return new string[] { version.AccessToken, version.RefreshToken };
    }

    public void SetAuthTokens(string access_token, string refresh_token)
    {
        Version.GetVersion().SetTokens(access_token, refresh_token);
    }
}