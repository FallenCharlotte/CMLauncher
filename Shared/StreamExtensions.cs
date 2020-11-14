using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    public static IEnumerable<Patch> Patches(this IReader source)
    {
        while (source.MoveToNextEntry())
        {
            if (!source.Entry.IsDirectory)
            {
                using (Patch patch = new Patch(source.Entry.Key))
                {
                    source.WriteEntryTo(patch.Stream);
                    patch.CacheLength();

                    yield return patch;
                }
            }
        }
    }

    public static ParallelQuery<Patch> WithProgressReporting(this ParallelQuery<Patch> source, long itemsCount, IProgress<float> progress, string prefix, Action<string> textUpdate)
    {
        int countShared = 0;
        return source.Select(item =>
        {
            int countLocal = Interlocked.Add(ref countShared, item.Length);
            progress.Report(countLocal / (float)itemsCount);
            textUpdate.Invoke($"{prefix} {item.FileName}");
            return item;
        });
    }
}

public class Patch : IDisposable
{
    public string FileName { get; set; }
    public MemoryStream Stream { get; private set; }
    public int Length { get; private set; }

    public Patch(string fileName)
    {
        FileName = fileName;
        Stream = new MemoryStream(100);
    }

    public void Dispose()
    {
        Stream.Dispose();
    }
    public void CacheLength()
    {
        Length = (int) Stream.Length;
    }
}