using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BsDiff;
using SharpCompress.Common;
using SharpCompress.Readers;
using SimpleJSON;

public class Main : IProgress<float>
{
    public static Main Instance { get; private set; }
    public Thread thread;

    private readonly ReleaseChannel useChannel = ReleaseChannel.Stable;
    private readonly IPlatformSpecific platformSpecific;
    private readonly string cdnUrl;
    private readonly CookieContainer cookieContainer = new CookieContainer();

    public Main(IPlatformSpecific platformSpecific)
    {
        this.platformSpecific = platformSpecific;
        Instance = this;

        var mainNode = platformSpecific.GetCMConfig();
        useChannel = mainNode["ReleaseChannel"].Value == "0" ? ReleaseChannel.Stable : ReleaseChannel.Dev;
        if (mainNode.HasKey("ReleaseServer"))
        {
            cdnUrl = mainNode["ReleaseServer"].Value;
        }
        else
        {
            cdnUrl = Config.CDN_URL;
        }

        thread = new Thread(() =>
        {
            DoUpdate();
        });
        thread.Start();
    }

    public async Task<int> GetLatestBuildNumber(ReleaseChannel releaseChannel)
    {
        using var httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
        using HttpClient client = new HttpClient(httpClientHandler);

        string channel = releaseChannel == ReleaseChannel.Stable ? "stable" : "dev";

        using (HttpResponseMessage response = await client.GetAsync($"{cdnUrl}/{channel}"))
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AuthException();
            }

            using (HttpContent content = response.Content)
            {
                return int.Parse(await content.ReadAsStringAsync());
            }
        }
    }

    private void SetVersion(int version)
    {
        platformSpecific.GetVersion().Update(version, cdnUrl);
    }

    public async void ResumeUpdate(string access_token, string refresh_token)
    {
        using HttpClient client = new HttpClient();

        try
        {
            using (HttpResponseMessage response = await client.GetAsync($"{Config.AUTH_URL}/cookie?access_token={access_token}&refresh_token={refresh_token}"))
            {
                using (HttpContent content = response.Content)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        platformSpecific.PerformAuth();
                        return;
                    }
                    else if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception();
                    }

                    var str = await content.ReadAsStringAsync();
                    var json = JSON.Parse(str);

                    if (json.HasKey("access_token") && json.HasKey("refresh_token"))
                    {
                        platformSpecific.SetAuthTokens(json["access_token"].Value, json["refresh_token"].Value);
                    }

                    cookieContainer.Add(new Uri(cdnUrl), new Cookie("Cloud-CDN-Cookie", json["cookie"].Value));
                    DoUpdate();
                }
            }
        }
        catch (Exception)
        {
            // Can't update because we can't auth, but can't auth. Give up.
            platformSpecific.Exit();
        }
    }

    private async void DoUpdate()
    {
        try
        {
            var version = platformSpecific.GetVersion();
            int current = version.VersionNumber;
            int desired = await GetLatestBuildNumber(useChannel);

            /**
             * Update if:
             *  - Our version server does not match (possibly because we have no current version)
             *  - We have an old version
             *  - We have a newer version but we want to be on the stable build
             */
            if (version.VersionServer != cdnUrl || current < desired || (current > desired && useChannel == ReleaseChannel.Stable))
            {
                PerformUpdate(current, desired);
                return;
            }
        }
        catch (AuthException)
        {
            CheckAuth();
            return;
        }
        catch (Exception) { };

        platformSpecific.Exit();
    }

    private void CheckAuth()
    {
        if (cookieContainer.Count == 0)
        {
            string[] tokens = platformSpecific.GetAuthTokens();

            if (tokens.Length > 1)
            {
                ResumeUpdate(tokens[0], tokens[1]);
                return;
            }
        }

        platformSpecific.PerformAuth();
    }

    private async void PerformUpdate(int current, int desired)
    {
        int stable = await GetLatestBuildNumber(ReleaseChannel.Stable);
        var version = platformSpecific.GetVersion();

        if (version.VersionServer != cdnUrl || current > desired)
        {
            current = await UpdateUsingZip(stable);
        }

        if (current < desired)
        {
            var patches = await FindPath(platformSpecific.GetCDNPrefix(), current, desired);

            if (patches == null)
            {
                // We can't patch to the desired version
                // If we're not on stable go there
                if (current != stable)
                {
                    await UpdateUsingZip(stable);
                }

                // Abandon ship!
                // Hopefully someone creates an update path for us next time around
                platformSpecific.Exit();
                return;
            }

            try
            {
                foreach (int patch in patches)
                {
                    current = await UpdateUsingPatch(current, patch);
                }
            }
            catch (xdelta3Exception)
            {
                // Files are almost certainly between builds so
                // require an update before running again
                SetVersion(0);

                // Bail back to stable
                await UpdateUsingZip(stable);
            }
        }

        platformSpecific.Exit();
    }

    private async Task<List<int>> FindPath(string prefix, int current, int desired)
    {
        var regex = new Regex(prefix + @"[0-9]+/([0-9]+).patch");
        var patches = new List<int>();

        using var httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
        using HttpClient client = new HttpClient(httpClientHandler);

        using (HttpResponseMessage response = await client.GetAsync($"{cdnUrl}?prefix={prefix}{desired}/"))
        {
            using (HttpContent content = response.Content)
            {
                var stream = await content.ReadAsStreamAsync();
                XmlReader xReader = XmlReader.Create(stream);

                while (xReader.ReadToFollowing("Contents"))
                {
                    xReader.ReadToFollowing("Key");
                    var str = xReader.ReadElementContentAsString();
                    var matches = regex.Matches(str);

                    if (matches.Count > 0)
                    {
                        int possible = int.Parse(matches[0].Groups[1].Value);
                        patches.Add(possible);
                    }
                }
            }
        }

        if (patches.Contains(current))
        {
            // We can patch from here!
            var ret = new List<int>
                {
                    desired
                };
            return ret;
        }

        try
        {
            var oldest = patches.Where(a => a > current).Min();
            var ret2 = await FindPath(prefix, current, oldest);

            if (ret2 != null)
                ret2.Add(desired);

            return ret2;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<int> UpdateUsingZip(int version)
    {
        platformSpecific.UpdateLabel("Downloading update...");
        string downloadUrl = Config.USE_CDN ? $"{cdnUrl}/{platformSpecific.GetCDNPrefix()}{version}/{platformSpecific.GetCDNFilename()}" :
            $"https://jenkins.kirkstall.top-cat.me/job/ChroMapper/{version}/artifact/{platformSpecific.GetJenkinsFilename()}";

        using (var tmp = new TempFile())
        {
            using var httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using HttpClient client = new HttpClient(httpClientHandler);

            client.Timeout = TimeSpan.FromMinutes(5);

            using (var file = new FileStream(tmp.Path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await client.DownloadAsync(downloadUrl, file, this);
            }

            ExtractZip(tmp.Path);

            SetVersion(version);

            return version;
        }
    }

    private void ExtractZip(string filename)
    {
        platformSpecific.UpdateLabel("Extracting zip");
        Report(0);

        string destinationDirectoryFullPath = platformSpecific.GetDownloadFolder();

        var opts = new ExtractionOptions()
        {
            Overwrite = true,
            PreserveFileTime = true
        };

        using (Stream stream = File.OpenRead(filename))
        {
            var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    string keyFilename = reader.Entry.Key;

                    platformSpecific.UpdateLabel($"Extracting {keyFilename.Replace("chromapper/", "").Replace("ChroMapper.app/", "")}");
                    platformSpecific.UpdateProgress(((float)stream.Position) / stream.Length);

                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, keyFilename));

                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Trying to extract file outside of destination directory");
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    if (keyFilename == "") // Assuming Empty for Directory
                    {
                        continue;
                    }

                    reader.WriteEntryToFile(completeFileName, opts);
                }
            }
        }
    }

    private async Task<int> UpdateUsingPatch(int source, int dest)
    {
        platformSpecific.UpdateLabel($"Downloading patch for {dest}");
        string downloadUrl = $"{cdnUrl}/{platformSpecific.GetCDNPrefix()}{dest}/{source}.patch";

        using (var tmp = new TempFile())
        {
            using var httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using HttpClient client = new HttpClient(httpClientHandler);

            client.Timeout = TimeSpan.FromMinutes(5);

            using (var file = new FileStream(tmp.Path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await client.DownloadAsync(downloadUrl, file, this);
            }

            ApplyPatch(tmp.Path);
            SetVersion(dest);

            return dest;
        }
    }

    private void ApplyPatch(string filename)
    {
        Report(0);
        using (Stream stream = File.OpenRead(filename))
        {
            var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    string keyFilename = reader.Entry.Key;
                    string patchFilename = keyFilename;
                    string compressionType = keyFilename.Substring(0, keyFilename.IndexOf("/"));

                    if (compressionType == "xdelta" || compressionType == "bsdiff")
                    {
                        keyFilename = keyFilename.Substring(compressionType.Length + 1);
                        patchFilename = patchFilename.Replace(compressionType, platformSpecific.LocalFolderName()).TrimStart('/');
                    }
                    else
                    {
                        compressionType = "";
                        patchFilename = Path.Combine(platformSpecific.LocalFolderName(), patchFilename);
                    }

                    patchFilename = Path.Combine(platformSpecific.GetDownloadFolder(), patchFilename);

                    platformSpecific.UpdateLabel($"Patching {keyFilename}");
                    Report(((float)stream.Position) / stream.Length);

                    using (MemoryStream memStream = new MemoryStream(100))
                    {
                        reader.WriteEntryTo(memStream);

                        byte[] newFile = memStream.ToArray();

                        if (compressionType == "xdelta")
                        {
                            byte[] oldFile = File.ReadAllBytes(patchFilename);
                            newFile = xdelta3.ApplyPatch(memStream.ToArray(), oldFile);
                        }
                        else if (compressionType == "bsdiff")
                        {
                            using (FileStream input = new FileStream(patchFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (MemoryStream memStream2 = new MemoryStream(100))
                            {
                                BinaryPatchUtility.Apply(input, () => new MemoryStream(newFile), memStream2);
                                newFile = memStream2.ToArray();
                            }
                        }
                        File.WriteAllBytes(patchFilename, newFile);
                    }
                }
            }
        }
    }

    public void Report(float value)
    {
        platformSpecific.UpdateProgress(value);
    }
}