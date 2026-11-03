namespace PenangGoApi.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? AccessibilityPreference { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}