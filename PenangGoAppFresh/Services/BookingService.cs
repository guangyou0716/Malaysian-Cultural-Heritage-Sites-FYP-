using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class BookingService : IBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public BookingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/bookings");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dtos = JsonSerializer.Deserialize<List<ApiBookingDto>>(content, _jsonOptions) ?? new();
                return dtos.Select(MapDto).ToList();
            }
            catch { return new List<Booking>(); }
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bookings/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dtos = JsonSerializer.Deserialize<List<ApiBookingDto>>(content, _jsonOptions) ?? new();
                return dtos.Select(MapDto).ToList();
            }
            catch { return new List<Booking>(); }
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bookings/{id}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<ApiBookingDto>(content, _jsonOptions);
                return dto != null ? MapDto(dto) : null;
            }
            catch { return null; }
        }

        public async Task<Booking?> CreateBookingAsync(Booking booking)
        {
            try
            {
                // The server derives the user from the access token; only the event is client-supplied
                var json = JsonSerializer.Serialize(new { eventId = booking.EventId }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/bookings", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<ApiBookingDto>(responseContent, _jsonOptions);
                return dto != null ? MapDto(dto) : null;
            }
            catch { return null; }
        }

        public async Task<Booking?> UpdateBookingAsync(Booking booking)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { status = booking.Status.ToString() }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/bookings/{booking.Id}", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<ApiBookingDto>(responseContent, _jsonOptions);
                return dto != null ? MapDto(dto) : null;
            }
            catch { return null; }
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { status = status.ToString() }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/bookings/{bookingId}/status", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<Booking?> CancelBookingAsync(int bookingId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/bookings/{bookingId}/cancel", null);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<ApiBookingDto>(responseContent, _jsonOptions);
                return dto != null ? MapDto(dto) : null;
            }
            catch { return null; }
        }

        public async Task<bool> DeleteBookingAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/bookings/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<Booking?> BookEventAsync(int userId, int eventId)
        {
            // userId is ignored: the server derives it from the access token
            return await CreateBookingAsync(new Booking { EventId = eventId });
        }

        private static Booking MapDto(ApiBookingDto dto) => new()
        {
            Id = dto.Id,
            UserId = dto.UserId,
            EventId = dto.EventId,
            BookingTime = dto.BookingTime,
            Status = Enum.TryParse<BookingStatus>(dto.Status, true, out var s) ? s : BookingStatus.Pending,
            User = dto.User == null ? null! : new User
            {
                Id = dto.User.Id,
                Name = dto.User.Name,
                Email = dto.User.Email
            },
            Event = dto.Event == null ? null! : new Event
            {
                Id = dto.Event.Id,
                Title = dto.Event.Title,
                Description = dto.Event.Description,
                StartTime = dto.Event.StartTime,
                EndTime = dto.Event.EndTime,
                Location = dto.Event.Location,
                Price = dto.Event.Price,
                MaxCapacity = dto.Event.MaxCapacity,
                CurrentBookings = dto.Event.CurrentBookings,
                IsActive = dto.Event.IsActive,
                IsTour = dto.Event.IsTour,
                ImageUrl = dto.Event.ImageUrl
            }
        };
    }

    internal class ApiBookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public ApiUserDto? User { get; set; }
        public ApiEventDto? Event { get; set; }
    }

    internal class ApiUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    internal class ApiEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentBookings { get; set; }
        public bool IsActive { get; set; }
        public bool IsTour { get; set; }
        public string? ImageUrl { get; set; }
    }
}