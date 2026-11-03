using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("site_visit_bookings")]
    public class SiteVisitBooking
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int HeritageSiteId { get; set; }

        public DateTime VisitDate { get; set; }

        [StringLength(20)]
        public string TimeSlot { get; set; } = "Morning";

        public int Quantity { get; set; } = 1;

        public decimal TotalAmount { get; set; } = 0;

        public SiteVisitStatus Status { get; set; } = SiteVisitStatus.Confirmed;

        [StringLength(32)]
        public string Reference { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual HeritageSite Site { get; set; } = null!;
    }

    public enum SiteVisitStatus
    {
        Confirmed,
        Cancelled,
        Completed
    }
}