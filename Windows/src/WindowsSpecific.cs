using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using CM_Launcher;
using SimpleJSON;

public class WindowsSpecific : IPlatformSpecific
{
    private readonly Updater _updater;
    private readonly string[] _args;

    public WindowsSpecific(Updater updater, string[] args)
    {
        _updater = updater;
        _args = args;
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

        if (!File.Exists(cmSettingsPath))
        {
            return new JSONObject();
        }

        using (StreamReader reader = new StreamReader(cmSettingsPath))
        {
            return JSON.Parse(reader.ReadToEnd());
        }
    }

    public string GetCDNPrefix() => "win/";

    public bool UseCDN() => !_args.Any(x => x.Equals("--no-cdn"));

    public void UpdateLabel(string label)
    {
        _updater.UpdateLabel(label);
    }

    public void UpdateProgress(float progress)
    {
        _updater.Report(progress);
    }

    public void Exit()
    {
        // Don't run CM as admin, return to unprivileged
        if (IsAdministrator())
        {
            Application.Exit();
            return;
        }

        var cmWindowStyle = ProcessWindowStyle.Normal;
        if (Updater.OriginalWindowState == FormWindowState.Maximized)
        {
            cmWindowStyle = ProcessWindowStyle.Maximized;
        }
        else if (Updater.OriginalWindowState == FormWindowState.Minimized)
        {
            cmWindowStyle = ProcessWindowStyle.Minimized;
        }

        var passthroughArgs = _args.Length == 0 ? "" : (" " + string.Join(" ", _args));

        var startInfo = new ProcessStartInfo("ChroMapper.exe")
        {
            WorkingDirectory = Path.Combine(GetDownloadFolder(), "chromapper"),
            Arguments = $"--launcher \"{GetCMLPath()}\"" + passthroughArgs,
            WindowStyle = cmWindowStyle
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
            Arguments = string.Join(" ", _args),
            WorkingDirectory = GetDownloadFolder()
        };

        Process.Start(startInfo);

        Application.Exit();
    }

    public static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void StartAsAdmin()
    {
        var startInfo = new ProcessStartInfo(AppDomain.CurrentDomain.FriendlyName)
        {
            WorkingDirectory = GetDownloadFolder(),
            UseShellExecute = true,
            Verb = "runas"
        };

        var process = Process.Start(startInfo);
        if (process == null) return;

        process.WaitForExit();
        process.Close();
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

    public string GetJenkinsFilename() => "ChroMapper-Win64.zip";

    public string GetCDNFilename() => "Win64.zip";

    public string GetDownloadFolder()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    public string LocalFolderName() => "chromapper";

    public string GetCMLFilename() => "CML.exe";

    private string GetTempCMLPath() => Path.Combine(GetDownloadFolder(), "CML.old.exe");

    private string GetCMLPath() => Path.Combine(GetDownloadFolder(), AppDomain.CurrentDomain.FriendlyName);
}