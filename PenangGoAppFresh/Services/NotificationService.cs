using System;

namespace PenangGoAppFresh.Services
{
    public class NotificationService
    {
        public event Action<string, string> OnNotificationReceived;
        public void RaiseNotification(string title, string body)
        {
            OnNotificationReceived?.Invoke(title, body);
        }
    }
}