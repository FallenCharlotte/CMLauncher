using System;
using SimpleJSON;

public interface IPlatformSpecific
{
    IVersion GetVersion();

    JSONNode GetCMConfig();

    string GetCDNPrefix();

    string GetJenkinsFilename();

    string GetCDNFilename();

    string GetDownloadFolder();

    string LocalFolderName();

    void PerformAuth();

    string[] GetAuthTokens();

    void SetAuthTokens(string access_token, string refresh_token);

    void UpdateLabel(string label);

    void UpdateProgress(float progress);

    void Exit();
}
