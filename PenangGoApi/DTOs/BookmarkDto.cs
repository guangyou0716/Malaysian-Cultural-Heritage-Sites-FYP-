namespace PenangGoApi.DTOs
{
    public class BookmarkDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public DateTime CreatedAt { get; set; }
        public HeritageSiteDto? Site { get; set; }
    }
}