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
        
        public string? ARModelUrl { get; set; }
        
        public bool HasPanoramaContent { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Additional properties for enhanced functionality
        public string? Category { get; set; } // e.g., "Temple", "Mosque", "Mansion", "Fort"
        
        public string? HistoricalPeriod { get; set; } // e.g., "19th Century", "Colonial Era"
        
        public string? CulturalSignificance { get; set; } // Description of cultural importance
        
        public string? VisitingHours { get; set; } // Opening hours information
        
        public string? EntryFee { get; set; } // Entry fee information
        
        public string? ContactInfo { get; set; } // Contact information
        
        public string? Website { get; set; } // Official website
        
        public bool IsAccessible { get; set; } = true; // Accessibility information
        
        public string? PhotoGallery { get; set; } // JSON array of photo URLs
        
        public double AverageRating { get; set; } = 0.0; // Calculated average rating
        
        public int TotalReviews { get; set; } = 0; // Total number of reviews
        
        public int TotalBookmarks { get; set; } = 0; // Total number of bookmarks
        
        // Navigation properties
        public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<PanoramaContent> PanoramaContents { get; set; } = new List<PanoramaContent>();
    }
}