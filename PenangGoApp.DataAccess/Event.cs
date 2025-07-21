using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("events")]
    public class Event
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        // Alias for Title to maintain compatibility with existing code
        public string Name => Title;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        // Compatibility properties for API
        public DateTime StartDate 
        { 
            get => StartTime; 
            set => StartTime = value; 
        }
        
        public DateTime EndDate 
        { 
            get => EndTime; 
            set => EndTime = value; 
        }
        
        // Alias for StartTime to maintain compatibility with existing code
        public DateTime Date => StartTime;
        
        [Required]
        public string Location { get; set; } = string.Empty;
        
        public int MaxCapacity { get; set; }
        
        public int CurrentBookings { get; set; } = 0;
        
        public decimal? Price { get; set; } // Make nullable for free events
        
        public bool IsActive { get; set; } = true;
        
        public bool IsTour { get; set; } = false;
        
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // --- New UI/feature fields ---
        public string? Type { get; set; } // e.g. Festival, Tour, Workshop, Exhibition
        public string? Icon { get; set; } // Emoji or icon string
        public string? Gradient { get; set; } // CSS gradient string for UI
        public bool IsPopular { get; set; } // Mark as popular event
        public bool IsNew { get; set; } // Mark as new event
        // --- End new fields ---
        
        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}