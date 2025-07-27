namespace PenangGoApi.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentBookings { get; set; }
        public decimal? Price { get; set; }
        public bool IsActive { get; set; }
        public bool IsTour { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 