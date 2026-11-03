using PenangGoApp.DataAccess;

namespace PenangGoApi.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? SiteName { get; set; }
        public string? UserName { get; set; }
    }
}