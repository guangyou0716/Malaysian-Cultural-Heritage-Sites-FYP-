using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("bookings")]
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public int EventId { get; set; }
        
        public DateTime BookingTime { get; set; } = DateTime.UtcNow;
        
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}