using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace SharpCompress.Readers
{
    public interface IEntry
    {
        bool IsValid { get; set; }
        bool IsDirectory { get; set; }
        string Key { get; set; }
    }

    public class Entry : IEntry
    {
        public Entry(TarEntry getNextEntry)
        {
            IsValid = getNextEntry != null;

            if (getNextEntry != null)
            {
                IsDirectory = getNextEntry.IsDirectory;
                Key = getNextEntry.Name;
            }
        }

        public bool IsValid { get; set; }
        public bool IsDirectory { get; set; }
        public string Key { get; set; }
    }
    
    public interface IReader
    {
        bool MoveToNextEntry();
        Entry Entry { get; set; }
        void WriteEntryTo(Stream stream);
    }

    public class Reader : IReader
    {
        private TarInputStream _tarArchive;

        public Reader(Stream stream)
        {
            Stream gzipStream = new GZipInputStream(stream);
            _tarArchive = new TarInputStream(gzipStream);
        }

        public bool MoveToNextEntry()
        {
            Entry = new Entry(_tarArchive.GetNextEntry());
            return Entry.IsValid;
        }

        public Entry Entry { get; set; }

        public void WriteEntryTo(Stream stream)
        {
            _tarArchive.CopyEntryContents(stream);
        }
    }
    
    public class ReaderFactory
    {
        public static IReader Open(Stream stream)
        {
            return new Reader(stream);
        }
    }
}