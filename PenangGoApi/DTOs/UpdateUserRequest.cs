namespace PenangGoApi.DTOs
{
    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? AccessibilityPreference { get; set; }
        public bool? EmailNotificationsEnabled { get; set; }
        public bool? PushNotificationsEnabled { get; set; }
    }
}
