using BsDiff;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

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
