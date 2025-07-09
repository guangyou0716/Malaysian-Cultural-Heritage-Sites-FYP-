using Android.Content;
using PenangGoApp;

namespace PenangGoApp.Platforms.Android
{
    public class ARService : IARService
    {
        public void OpenARView()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(ARActivity));
            intent.SetFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
    }
} 