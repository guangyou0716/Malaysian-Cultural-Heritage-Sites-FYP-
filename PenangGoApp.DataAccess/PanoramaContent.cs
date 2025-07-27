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
        
        // Approval system fields
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved; // Default to approved for admin-created content
        
        public DateTime? SubmittedAt { get; set; } // When the panorama was submitted (same as UploadedAt for user submissions)
        
        public int? ApprovedBy { get; set; } // Admin who approved/rejected the panorama
        
        public DateTime? ApprovedAt { get; set; } // When the panorama was approved/rejected
        
        public string? ApprovalNotes { get; set; } // Admin notes for approval/rejection
        
        // Navigation property for approval system
        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }
    }
}