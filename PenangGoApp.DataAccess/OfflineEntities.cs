using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("offline_outbox")]
    public class OfflineOutboxItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(32)]
        public string Operation { get; set; } = string.Empty; // BookmarkAdd | BookmarkRemove

        public int? SiteId { get; set; }

        public int? BookmarkId { get; set; } // server bookmark id for BookmarkRemove

        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("offline_sync_meta")]
    public class OfflineSyncMeta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}