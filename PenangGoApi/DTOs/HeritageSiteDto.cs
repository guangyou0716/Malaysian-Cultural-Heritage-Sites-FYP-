namespace PenangGoApi.DTOs
{
    public class HeritageSiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? HistoricalPeriod { get; set; }
        public string? ImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasPanoramaContent { get; set; }
    }
} 