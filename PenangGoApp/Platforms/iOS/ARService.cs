using UIKit;
using PenangGoApp;

namespace PenangGoApp.Platforms.iOS
{
    public class ARService : IARService
    {
        public void OpenARView()
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            var rootVC = window.RootViewController;
            var arVC = new ARViewController();
            rootVC.PresentViewController(arVC, true, null);
        }
    }
} 