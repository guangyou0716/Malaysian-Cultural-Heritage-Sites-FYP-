namespace PenangGoApi.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public EventDto? Event { get; set; }
    }
} 