using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class BookmarkService
    {
        private readonly PenangGoContext _context;
        public BookmarkService(PenangGoContext context)
        {
            _context = context;
        }

        public async Task<List<Bookmark>> GetUserBookmarksAsync(int userId)
        {
            return await _context.Bookmarks.Where(b => b.UserId == userId).ToListAsync();
        }

        public async Task AddBookmarkAsync(int userId, int siteId)
        {
            if (!await _context.Bookmarks.AnyAsync(b => b.UserId == userId && b.SiteId == siteId))
            {
                _context.Bookmarks.Add(new Bookmark { UserId = userId, SiteId = siteId });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveBookmarkAsync(int userId, int siteId)
        {
            var bookmark = await _context.Bookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.SiteId == siteId);
            if (bookmark != null)
            {
                _context.Bookmarks.Remove(bookmark);
                await _context.SaveChangesAsync();
            }
        }
    }
} 