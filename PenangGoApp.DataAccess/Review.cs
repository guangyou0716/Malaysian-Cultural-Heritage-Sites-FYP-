using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public int SiteId { get; set; }
        
        public int Rating { get; set; }
        
        [Required]
        public string Comment { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual HeritageSite Site { get; set; } = null!;
    }
}