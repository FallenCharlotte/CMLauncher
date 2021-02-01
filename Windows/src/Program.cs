using System;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Sentry;
using Sentry.Protocol;

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
            using (SentrySdk.Init("https://76dcf1f2484f4839a78b3713420b5147@o462013.ingest.sentry.io/5556322"))
            {
                using (new Mutex(true, "CMLauncher", out var createdNew))
                {
                    if (!createdNew) return;

                    var identity = WindowsIdentity.GetCurrent();
                    SentrySdk.ConfigureScope(scope =>
                    {
                        scope.User = new User
                        {
                            Username = identity.Name
                        };
                    });

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Updater());
                }
            }
        }
    }
}
