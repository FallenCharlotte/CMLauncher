using SimpleJSON;

public interface IPlatformSpecific
{
    IVersion GetVersion();

    JSONNode GetCMConfig();

    string GetCDNPrefix();

    string GetJenkinsFilename();

    string GetCDNFilename();

    string GetCMLFilename();

    string GetDownloadFolder();

    string LocalFolderName();

    void UpdateLabel(string label);

    void UpdateProgress(float progress);

    void Exit();

    void Restart(string tmpFile);

    void CleanupUpdate();
}
