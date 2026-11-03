using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("heritage_sites")]
    public class HeritageSite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? ImageUrl { get; set; }

        public bool HasPanoramaContent { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Approval system fields
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved; // Default to approved for admin-created sites

        public int? SubmittedBy { get; set; } // User who submitted the site

        public DateTime? SubmittedAt { get; set; } // When the site was submitted

        public int? ApprovedBy { get; set; } // Admin who approved/rejected the site

        public DateTime? ApprovedAt { get; set; } // When the site was approved/rejected

        public string? ApprovalNotes { get; set; } // Admin notes for approval/rejection

        // Additional properties for enhanced functionality
        public string? Category { get; set; } // e.g., "Temple", "Mosque", "Mansion", "Fort"

        public string? HistoricalPeriod { get; set; } // e.g., "19th Century", "Colonial Era"

        public string? CulturalSignificance { get; set; } // Description of cultural importance

        public string? VisitingHours { get; set; } // Opening hours information

        public string? EntryFee { get; set; } // Entry fee information

        public string? ContactInfo { get; set; } // Contact information

        public string? Website { get; set; } // Official website

        public bool IsAccessible { get; set; } = true; // Accessibility information

        public bool RequiresReservation { get; set; } = false;

        public int? DailyCapacity { get; set; } // Per-day visitor cap for reservation-required sites

        public double AverageRating { get; set; } = 0.0; // Calculated average rating

        public int TotalReviews { get; set; } = 0; // Total number of reviews

        public int TotalBookmarks { get; set; } = 0; // Total number of bookmarks

        // Navigation properties
        public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<PanoramaContent> PanoramaContents { get; set; } = new List<PanoramaContent>();
        public virtual ICollection<SiteVisitBooking> SiteVisitBookings { get; set; } = new List<SiteVisitBooking>();

        // Navigation properties for approval system
        [ForeignKey("SubmittedBy")]
        public virtual User? Submitter { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }
    }

    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}