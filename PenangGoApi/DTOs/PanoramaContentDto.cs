namespace PenangGoApi.DTOs
{
    public class PanoramaContentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string PanoramaUrl { get; set; }
        public int HeritageSiteId { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
} 