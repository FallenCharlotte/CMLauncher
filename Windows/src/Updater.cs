using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CM_Launcher
{
    public partial class Updater : Form
    {
        private readonly SynchronizationContext synchronizationContext;
        public static FormWindowState OriginalWindowState;

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

        protected override void OnLoad(EventArgs e)
        {
            OriginalWindowState = WindowState;
            WindowState = FormWindowState.Normal;
            base.OnLoad(e);
        }

        public Updater()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            var specific = new WindowsSpecific(this);
            int tries = 0;
            while (tries++ < 3)
            {
                if (!FilesLocked(specific))
                {
                    new Main(specific);
                    return;
                }
                Thread.Sleep(1000 * tries);
            }

            new Thread(() =>
            {
                specific.Exit();
            }).Start();
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
