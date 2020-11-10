using System;
using System.IO;

class Version : IVersion
{
    public int VersionNumber { get; private set; } = 0;
    public string VersionServer { get; private set; } = "";

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
            string[] versionInfo = File.ReadAllText(VersionFilename).Split('\n');
            VersionNumber = int.Parse(versionInfo[0]);

            if (versionInfo.Length > 1)
                VersionServer = versionInfo[1];
        }
    }

    void IVersion.Update(int version, string server)
    {
        File.WriteAllText(VersionFilename, $"{version}\n{server}");

        VersionNumber = version;
        VersionServer = server;
    }
}
