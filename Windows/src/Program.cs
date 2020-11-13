using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace CM_Launcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			RegisterProtocolHandler();

			string uri = null;
			if (args.Length > 0)
			{
				uri = args[0].Trim();
			}

			IUriHandler handler = UriHandler.GetHandler();
			if (handler != null)
			{
				if (uri != null) handler.HandleUri(uri);
			}
			else
			{
				UriHandler.Register();

				if (uri != null)
				{
					new UriHandler().HandleUri(uri, true);
				}

				Application.Run(new Updater());
			}
		}

        private static void RegisterProtocolHandler()
        {
			const string protocolValue = "cml:CM Launcher";
			Registry.SetValue(
				@"HKEY_CURRENT_USER\Software\Classes\cml",
				string.Empty,
				protocolValue,
				RegistryValueKind.String);
			Registry.SetValue(
				@"HKEY_CURRENT_USER\Software\Classes\cml",
				"URL Protocol",
				string.Empty,
				RegistryValueKind.String);

			string command = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
			Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\cml\shell\open\command", string.Empty, $"\"{command}\" \"%1\"", RegistryValueKind.String);
		}
    }
}
