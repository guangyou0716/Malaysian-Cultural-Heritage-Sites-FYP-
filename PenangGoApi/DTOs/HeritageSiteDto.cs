using PenangGoApp.DataAccess;

namespace PenangGoApi.DTOs
{
    public class HeritageSiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? HistoricalPeriod { get; set; }
        public string? ImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasPanoramaContent { get; set; }
        public bool RequiresReservation { get; set; }
        public int? DailyCapacity { get; set; }

        // Approval system fields
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
    }
}