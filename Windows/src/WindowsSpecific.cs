using System;
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
            Arguments = $"--launcher \"{GetCMLPath()}\""
        };

        Process.Start(startInfo);

        Application.Exit();
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

        Application.Exit();
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

    public string GetCMLFilename()
    {
        return "CML.exe";
    }

    private string GetTempCMLPath()
    {
        return Path.Combine(GetDownloadFolder(), "CML.old.exe");
    }

    private string GetCMLPath()
    {
        return Path.Combine(GetDownloadFolder(), AppDomain.CurrentDomain.FriendlyName);
    }
}