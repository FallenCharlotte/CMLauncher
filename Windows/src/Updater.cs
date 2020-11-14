using System;
using System.Threading;
using System.Windows.Forms;

namespace CM_Launcher
{
    public partial class Updater : Form
    {
        private readonly SynchronizationContext synchronizationContext;

        public Updater()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            new Main(new WindowsSpecific(this));
        }

        public void UpdateLabel(string text)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                label1.Text = $"{o}";
            }), text);
        }

        public void Report(float value)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                progressBar1.Value = Math.Min((int) o, 1000);
            }), (int)(value * 1000));
        }
    }
}
