using SimpleJSON;
using System;
using System.IO;

class Version : IVersion
{
    private readonly JSONNode versionInfo;

    public int VersionNumber { get; private set; } = 0;
    public string VersionServer { get; private set; } = "";
    public string AccessToken { get; private set; } = "";
    public string RefreshToken { get; private set; } = "";

    private readonly string VersionFilename;
    private static Version version = null;

    public static Version GetVersion()
    {
        if (version == null)
        {
            version = new Version();
        }
        return version;
    }

    private Version()
    {
        VersionFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cm-version");

        if (File.Exists(VersionFilename))
        {
            versionInfo = JSON.Parse(File.ReadAllText(VersionFilename));
            VersionNumber = versionInfo["version"]?.AsInt ?? 0;

            if (versionInfo.HasKey("server"))
                VersionServer = versionInfo["server"].Value;

            if (versionInfo.HasKey("access_token"))
                AccessToken = versionInfo["access_token"].Value;

            if (versionInfo.HasKey("refresh_token"))
                RefreshToken = versionInfo["refresh_token"].Value;
        }
    }

    public void SetTokens(string accessToken, string refreshToken)
    {
        versionInfo["access_token"] = accessToken;
        versionInfo["refresh_token"] = refreshToken;

        Save();

        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    private void Save()
    {
        File.WriteAllText(VersionFilename, versionInfo.ToString());
    }

    void IVersion.Update(int version, string server)
    {
        versionInfo["server"] = server;
        versionInfo["version"] = version;

        Save();

        VersionNumber = version;
        VersionServer = server;
    }
}
