using SimpleJSON;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CM_Launcher
{
    class Main
    {
        public static readonly bool USE_CDN = true;
        public static string CDN_URL = "https://cm.topc.at";
        public static readonly string LauncherFolder = AppDomain.CurrentDomain.BaseDirectory;

        private readonly Updater form;

        private readonly ReleaseChannel useChannel = ReleaseChannel.Stable;

        string GetKnownFolderPath(Guid knownFolderId)
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

        public Main(Updater form)
        {
            this.form = form;

            Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            string cmSettingsPath = Path.Combine(GetKnownFolderPath(localLowId), "BinaryElement", "ChroMapper", "ChroMapperSettings.json");

            using (StreamReader reader = new StreamReader(cmSettingsPath))
            {
                JSONNode mainNode = JSON.Parse(reader.ReadToEnd());
                useChannel = mainNode["ReleaseChannel"].Value == "0" ? ReleaseChannel.Stable : ReleaseChannel.Dev;
                if (mainNode.HasKey("ReleaseServer"))
                {
                    CDN_URL = mainNode["ReleaseServer"].Value;
                }
            }

            DoUpdate();
        }

        public static async Task<int> GetLatestBuildNumber(ReleaseChannel channelEnum)
        {
            using (HttpClient client = new HttpClient())
            {
                string channel = channelEnum == ReleaseChannel.Stable ? "stable" : "dev";

                using (HttpResponseMessage response = await client.GetAsync($"{CDN_URL}/{channel}"))
                {
                    using (HttpContent content = response.Content)
                    {
                        return int.Parse(await content.ReadAsStringAsync());
                    }
                }
            }
        }

        private async void DoUpdate()
        {
            try
            {
                Version version = Version.GetVersion();
                int current = version.VersionNumber;
                int desired = await GetLatestBuildNumber(useChannel);

                /**
                 * Update if:
                 *  - Our version server does not match (possibly because we have no current version)
                 *  - We have an old version
                 *  - We have a newer version but we want to be on the stable build
                 */
                if (version.VersionServer != CDN_URL || current < desired || (current > desired && useChannel == ReleaseChannel.Stable))
                {
                    form.Show(current, desired);
                    return;
                }
            }
            catch (Exception) { };

            Exit();
        }

        public static void Exit()
        {
            var startInfo = new ProcessStartInfo("ChroMapper.exe")
            {
                WorkingDirectory = Path.Combine(LauncherFolder, "chromapper")
            };

            Process.Start(startInfo);

            Application.Exit();
        }
    }
}
