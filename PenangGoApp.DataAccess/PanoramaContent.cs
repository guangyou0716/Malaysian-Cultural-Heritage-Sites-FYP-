using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("panorama_content")]
    public class PanoramaContent
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public string PanoramaUrl { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        [ForeignKey("UploadedBy")]
        public User Uploader { get; set; }

        // Link to HeritageSite
        public int? HeritageSiteId { get; set; }
        [ForeignKey("HeritageSiteId")]
        public HeritageSite HeritageSite { get; set; }
    }
}