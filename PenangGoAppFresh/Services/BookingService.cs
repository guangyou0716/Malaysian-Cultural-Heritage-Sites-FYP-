using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class BookingService : IBookingService
    {
        private readonly PenangGoContext _context;
        
        public BookingService(PenangGoContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingTime)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Booking?> CreateBookingAsync(Booking booking)
        {
            booking.BookingTime = DateTime.Now;
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<Booking?> UpdateBookingAsync(Booking booking)
        {
            var existingBooking = await _context.Bookings.FindAsync(booking.Id);
            if (existingBooking == null)
                return null;

            existingBooking.Status = booking.Status;
            existingBooking.BookingTime = booking.BookingTime;
            
            await _context.SaveChangesAsync();
            return existingBooking;
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return false;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Booking?> BookEventAsync(int userId, int eventId)
        {
            var booking = new Booking
            {
                UserId = userId,
                EventId = eventId,
                BookingTime = DateTime.Now,
                Status = BookingStatus.Confirmed
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }
    }
} 