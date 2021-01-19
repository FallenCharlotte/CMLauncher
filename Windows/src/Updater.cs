using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CM_Launcher
{
    public partial class Updater : Form
    {
        private readonly SynchronizationContext synchronizationContext;

        private static bool FilesLocked(IPlatformSpecific specific)
        {
            var file = Path.Combine(specific.GetDownloadFolder(), "chromapper", "ChroMapper.exe");
            try
            {
                if (File.Exists(file))
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        stream.Close();
                    }
                }
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }

        public Updater()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            var specific = new WindowsSpecific(this);
            if (FilesLocked(specific))
            {
                new Thread(() =>
                {
                    specific.Exit();
                }).Start();
            }
            else
            {
                new Main(specific);
            }
        }

        public void UpdateLabel(string text)
        {
            synchronizationContext.Post(o =>
            {
                label1.Text = $"{o}";
            }, text);
        }

        public void Report(float value)
        {
            synchronizationContext.Post(o =>
            {
                progressBar1.Value = Math.Min((int) o, 1000);
            }, (int)(value * 1000));
        }
    }
}
