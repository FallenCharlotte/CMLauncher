using System;
using System.Threading;
using System.Windows.Forms;

namespace CM_Launcher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            using (var mutex = new Mutex(true, "CMLauncher", out var createdNew))
            {
                if (!createdNew) return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Updater());
            }
        }
    }
}
