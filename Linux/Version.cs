using SimpleJSON;
using System;
using System.IO;

class Version : IVersion
{
    private readonly JSONObject versionInfo = new JSONObject();
    public int VersionNumber { get; private set; } = 0;
    public string VersionServer { get; private set; } = "";

    private readonly string versionFilename;
    private static Version _version;

    public static Version GetVersion(string path)
    {
        return _version ?? (_version = new Version(path));
    }

    private Version(string path)
    {
        versionFilename = Path.Combine(path, "cm-version");

        if (!File.Exists(versionFilename)) return;

        var json = JSON.Parse(File.ReadAllText(versionFilename));
        if (json is JSONObject j)
        {
            versionInfo = j;

            VersionNumber = versionInfo["version"]?.AsInt ?? 0;
            VersionServer = versionInfo["server"].Value;
        }
        else
        {
            var oldVersionInfo = File.ReadAllText(versionFilename).Split('\n');
            VersionNumber = int.Parse(oldVersionInfo[0]);

            if (oldVersionInfo.Length > 1)
                VersionServer = oldVersionInfo[1];
        }
    }

    void IVersion.Update(int version, string server)
    {
        versionInfo["server"] = server;
        versionInfo["version"] = version;

        File.WriteAllText(versionFilename, versionInfo.ToString());

        VersionNumber = version;
        VersionServer = server;
    }
}
