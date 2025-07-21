using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public interface IBookingService
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<Booking?> GetBookingByIdAsync(int id);
        Task<Booking?> CreateBookingAsync(Booking booking);
        Task<Booking?> UpdateBookingAsync(Booking booking);
        Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status);
        Task<bool> DeleteBookingAsync(int id);
        Task BookEventAsync(int userId, int eventId);
    }
} 