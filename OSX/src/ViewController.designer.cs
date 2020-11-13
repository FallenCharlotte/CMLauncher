// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace OSX
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSProgressIndicator progressBar { get; set; }

		[Outlet]
		AppKit.NSTextField progressLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (progressBar != null) {
				progressBar.Dispose ();
				progressBar = null;
			}

			if (progressLabel != null) {
				progressLabel.Dispose ();
				progressLabel = null;
			}
		}
	}
}
