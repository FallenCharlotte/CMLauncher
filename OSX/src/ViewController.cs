using System;

using AppKit;
using Foundation;
using Xamarin.Essentials;

namespace OSX
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Do any additional setup after loading the view.

            progressLabel.StringValue = "Checking for updates";
            progressBar.Indeterminate = true;

            //ver.Update(0, "");
            new Main(new MacSpecific(progressLabel, progressBar));

            // \o/
        }

        public override void ViewDidDisappear()
        {
            base.ViewDidDisappear();
            NSApplication.SharedApplication.Terminate(NSApplication.SharedApplication);
        }

        public override void ViewWillAppear()
        {
            base.ViewWillAppear();

            var window = View.Window;
            window.Center();
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
            }
        }
    }
}
