using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("bookmarks")]
    public class Bookmark
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int SiteId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual HeritageSite Site { get; set; } = null!;
    }
}