using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BsDiff;
using SharpCompress.Common;
using SharpCompress.Readers;

public class Main : IProgress<float>
{
    private readonly ReleaseChannel useChannel = ReleaseChannel.Stable;
    private readonly IPlatformSpecific platformSpecific;
    private readonly string cdnUrl;

    public Main(IPlatformSpecific platformSpecific)
    {
        this.platformSpecific = platformSpecific;

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

        new Thread(() =>
        {
            DoUpdate();
        }).Start();
    }

    public async Task<int> GetLatestBuildNumber(ReleaseChannel releaseChannel)
    {
        using (HttpClient client = new HttpClient())
        {
            string channel = releaseChannel == ReleaseChannel.Stable ? "stable" : "dev";

            using (HttpResponseMessage response = await client.GetAsync($"{cdnUrl}/{channel}"))
            {
                using (HttpContent content = response.Content)
                {
                    return int.Parse(await content.ReadAsStringAsync());
                }
            }
        }
    }

    private void SetVersion(int version)
    {
        platformSpecific.GetVersion().Update(version, cdnUrl);
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
        catch (Exception) { };

        platformSpecific.Exit();
    }

    private async void PerformUpdate(int current, int desired)
    {
        int stable = await GetLatestBuildNumber(ReleaseChannel.Stable);
        var version = platformSpecific.GetVersion();

        // Downgrade or first run
        if (version.VersionServer != cdnUrl || current > desired)
        {
            current = await UpdateUsingZip(stable);
        }

        // We need to update
        if (current < desired)
        {
            var patches = await FindPath(platformSpecific.GetCDNPrefix(), current, desired);

            // That's a lot of patches
            if (current < stable && (patches == null || patches.Count > Config.PATCH_SKIP_LIMIT))
            {
                if (stable == desired)
                {
                    // Too many patches just download it, tiggers the zip update code path below
                    patches = null;
                }
                else
                {
                    // Check how many patches we would save
                    var currentPatches = await FindPath(platformSpecific.GetCDNPrefix(), stable, desired);
                    if (patches == null || patches.Count - currentPatches.Count > Config.PATCH_SKIP_LIMIT)
                    {
                        // We'll save having to do many patches if we download the stable zip
                        current = await UpdateUsingZip(stable);
                        patches = currentPatches;
                    }
                }
            }

            if (patches == null)
            {
                // We can't patch to the desired version
                // If we're not on stable go there
                if (current != stable)
                {
                    current = await UpdateUsingZip(stable);
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
            catch (AggregateException)
            {
                // Files are almost certainly between builds so
                // require an update before running again
                SetVersion(0);

                // Bail back to stable
                current = await UpdateUsingZip(stable);
            }
        }

        platformSpecific.Exit();
    }

    private async Task<List<int>> FindPath(string prefix, int current, int desired)
    {
        var regex = new Regex(prefix + @"[0-9]+/([0-9]+).patch");
        var patches = new List<int>();

        using (HttpClient client = new HttpClient())
        {
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
        else if (patches.Count > 50)
        {
            // You're just asking for a stack overflow exception
            return null;
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
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var file = new FileStream(tmp.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(downloadUrl, file, this);
                }
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
            reader.Patches().AsParallel().Select(patch =>
            {
                string keyFilename = patch.FileName;

                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, keyFilename));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Lets just ignore it
                    //throw new IOException("Trying to extract file outside of destination directory");
                }
                else if (keyFilename == "")
                {
                    // Probably a directory
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    File.WriteAllBytes(completeFileName, patch.Stream.ToArray());
                }

                patch.FileName = patch.FileName.Replace("chromapper/", "").Replace("ChroMapper.app/", "");
                return patch;
            }).WithProgressReporting(stream.Length, this, "Extracting", platformSpecific.UpdateLabel).ForAll(p => { });
        }
    }

    private async Task<int> UpdateUsingPatch(int source, int dest)
    {
        platformSpecific.UpdateLabel($"Downloading patch for {dest}");
        string downloadUrl = $"{cdnUrl}/{platformSpecific.GetCDNPrefix()}{dest}/{source}.patch";

        using (var tmp = new TempFile())
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var file = new FileStream(tmp.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(downloadUrl, file, this);
                }
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
            reader.Patches().AsParallel().Select(patch =>
            {
                MemoryStream memStream = patch.Stream;
                string keyFilename = patch.FileName;

                string patchFilename = keyFilename;
                string compressionType = keyFilename.Substring(0, keyFilename.IndexOf("/"));

                if (compressionType == "xdelta" || compressionType == "bsdiff")
                {
                    patch.FileName = keyFilename = keyFilename.Substring(compressionType.Length + 1);
                    patchFilename = patchFilename.Replace(compressionType, platformSpecific.LocalFolderName()).TrimStart('/');
                }
                else
                {
                    compressionType = "";
                    patchFilename = Path.Combine(platformSpecific.LocalFolderName(), patchFilename);
                }

                patchFilename = Path.Combine(platformSpecific.GetDownloadFolder(), patchFilename);

                byte[] newFile = memStream.ToArray();

                if (compressionType == "xdelta")
                {
                    byte[] oldFile = File.Exists(patchFilename) ? File.ReadAllBytes(patchFilename) : new byte[] { };
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
                if (!Directory.Exists(patchFilename))
                    Directory.CreateDirectory(Path.GetDirectoryName(patchFilename));
                File.WriteAllBytes(patchFilename, newFile);

                return patch;
            }).WithProgressReporting(stream.Length, this, "Patching", platformSpecific.UpdateLabel).ForAll(p => { });
        }
    }

    public void Report(float value)
    {
        platformSpecific.UpdateProgress(value);
    }
}