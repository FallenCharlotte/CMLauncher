using AppKit;
using Foundation;
using WebKit;
using Xamarin.Essentials;

namespace OSX
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this, new ObjCRuntime.Selector("handleGetURLEvent:withReplyEvent:"), AEEventClass.Internet, AEEventID.GetUrl);
        }

        [Export("handleGetURLEvent:withReplyEvent:")]
        private void HandleGetURLEvent(NSAppleEventDescriptor descriptor, NSAppleEventDescriptor replyEvent)
        {
            string keyDirectObject = "----";
            uint keyword = (((uint)keyDirectObject[0]) << 24 |
                           ((uint)keyDirectObject[1]) << 16 |
                           ((uint)keyDirectObject[2]) << 8 |
                           ((uint)keyDirectObject[3]));
            string urlString = descriptor.ParamDescriptorForKeyword(keyword).StringValue;
            string[] token = urlString.Substring(6).Split("%7C");

            Preferences.Set("access_token", token[0]);
            Preferences.Set("refresh_token", token[1]);
            Main.Instance.ResumeUpdate(token[0], token[1]);
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}
