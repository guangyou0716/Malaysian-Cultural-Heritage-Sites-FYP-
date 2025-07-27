using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;
using Microsoft.Maui.Devices;

namespace PenangGoAppFresh.Services
{
    public interface IApiBookingService
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<Booking?> GetBookingByIdAsync(int id);
        Task<Booking?> CreateBookingAsync(Booking booking);
        Task<Booking?> UpdateBookingAsync(Booking booking);
        Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status);
        Task<bool> DeleteBookingAsync(int id);
        Task<Booking?> BookEventAsync(int userId, int eventId);
    }

    public class ApiBookingService : IApiBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiBookingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static string BaseUrl
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return DeviceInfo.DeviceType == DeviceType.Physical 
                        ? "http://192.168.0.20:7000/api" // Physical Android device
                        : "http://10.0.2.2:7000/api"; // Android emulator
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    return "http://localhost:7000/api"; // iOS simulator
                }
                else if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    return "http://localhost:7000/api"; // Windows
                }
                else if (DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    return "http://localhost:7000/api"; // macOS
                }
                else
                {
                    return "http://localhost:7000/api"; // Default fallback
                }
            }
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/bookings");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var bookingDtos = JsonSerializer.Deserialize<List<BookingDto>>(content, _jsonOptions) ?? new List<BookingDto>();
                
                // Convert DTOs to Booking objects
                var bookings = new List<Booking>();
                foreach (var dto in bookingDtos)
                {
                    bookings.Add(ConvertDtoToBooking(dto));
                }
                
                return bookings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all bookings: {ex.Message}");
                return new List<Booking>();
            }
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/bookings/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var bookingDtos = JsonSerializer.Deserialize<List<BookingDto>>(content, _jsonOptions) ?? new List<BookingDto>();
                
                // Convert DTOs to Booking objects
                var bookings = new List<Booking>();
                foreach (var dto in bookingDtos)
                {
                    bookings.Add(ConvertDtoToBooking(dto));
                }
                
                return bookings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user bookings: {ex.Message}");
                return new List<Booking>();
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/bookings/{id}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dto = JsonSerializer.Deserialize<BookingDto>(content, _jsonOptions);
                
                return dto != null ? ConvertDtoToBooking(dto) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting booking by id: {ex.Message}");
                return null;
            }
        }

        public async Task<Booking?> CreateBookingAsync(Booking booking)
        {
            try
            {
                Console.WriteLine($"CreateBookingAsync: Sending request to {BaseUrl}/bookings");
                var json = JsonSerializer.Serialize(booking, _jsonOptions);
                Console.WriteLine($"CreateBookingAsync: Request JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/bookings", content);
                
                Console.WriteLine($"CreateBookingAsync: Response status: {response.StatusCode}");
                Console.WriteLine($"CreateBookingAsync: Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"CreateBookingAsync: Response content: {responseContent}");
                
                var bookingDto = JsonSerializer.Deserialize<BookingDto>(responseContent, _jsonOptions);
                Console.WriteLine($"CreateBookingAsync: Deserialized DTO - {(bookingDto != null ? $"Success - ID: {bookingDto.Id}" : "Failed - null")}");
                
                return bookingDto != null ? ConvertDtoToBooking(bookingDto) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateBookingAsync: Error creating booking: {ex.Message}");
                Console.WriteLine($"CreateBookingAsync: Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<Booking?> UpdateBookingAsync(Booking booking)
        {
            try
            {
                var json = JsonSerializer.Serialize(booking, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/bookings/{booking.Id}", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var bookingDto = JsonSerializer.Deserialize<BookingDto>(responseContent, _jsonOptions);
                return bookingDto != null ? ConvertDtoToBooking(bookingDto) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating booking: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus status)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { status }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/bookings/{bookingId}/status", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating booking status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteBookingAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/bookings/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting booking: {ex.Message}");
                return false;
            }
        }

        public async Task<Booking?> BookEventAsync(int userId, int eventId)
        {
            try
            {
                Console.WriteLine($"ApiBookingService: Creating booking for user {userId}, event {eventId}");
                var booking = new Booking
                {
                    UserId = userId,
                    EventId = eventId,
                    BookingTime = DateTime.Now,
                    Status = BookingStatus.Confirmed
                };

                var result = await CreateBookingAsync(booking);
                Console.WriteLine($"ApiBookingService: Booking result - {(result != null ? $"Success - ID: {result.Id}" : "Failed - null returned")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiBookingService: Error booking event: {ex.Message}");
                Console.WriteLine($"ApiBookingService: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private Booking ConvertDtoToBooking(BookingDto dto)
        {
            return new Booking
            {
                Id = dto.Id,
                UserId = dto.UserId,
                EventId = dto.EventId,
                BookingTime = dto.BookingTime,
                Status = Enum.Parse<BookingStatus>(dto.Status),
                User = dto.User != null ? new PenangGoApp.DataAccess.User
                {
                    Id = dto.User.Id,
                    Name = dto.User.Name,
                    Email = dto.User.Email,
                    IsAdmin = dto.User.IsAdmin,
                    IsActive = dto.User.IsActive,
                    PhoneNumber = dto.User.PhoneNumber,
                    Bio = dto.User.Bio,
                    CreatedAt = dto.User.CreatedAt
                } : null,
                Event = dto.Event != null ? new PenangGoApp.DataAccess.Event
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
                    ImageUrl = dto.Event.ImageUrl,
                    CreatedAt = dto.Event.CreatedAt,
                    UpdatedAt = dto.Event.UpdatedAt
                } : null
            };
        }
    }

    // DTO class for API responses
    public class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public EventDto? Event { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public decimal? Price { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentBookings { get; set; }
        public bool IsActive { get; set; }
        public bool IsTour { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 