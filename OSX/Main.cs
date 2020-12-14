using AppKit;
using Sentry;

namespace OSX
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            using (SentrySdk.Init("https://76dcf1f2484f4839a78b3713420b5147@o462013.ingest.sentry.io/5556322"))
            {
                NSApplication.Init();
                NSApplication.Main(args);
            }
        }
    }
}
