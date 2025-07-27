using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using PenangGoApp.DataAccess;

namespace PenangGoApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendBookingConfirmationEmailAsync(Booking booking, User user, Event eventItem);
        Task<bool> SendBookingCancellationEmailAsync(Booking booking, User user, Event eventItem);
        Task<bool> SendBookingStatusUpdateEmailAsync(Booking booking, User user, Event eventItem, BookingStatus newStatus);
        Task<bool> SendSiteApprovalEmailAsync(User user, HeritageSite site);
        Task<bool> SendSiteRejectionEmailAsync(User user, HeritageSite site, string reason);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:Username"] ?? "";
            _smtpPassword = _configuration["Email:Password"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@penanggo.com";
            _fromName = _configuration["Email:FromName"] ?? "PenangGo";
        }

        public async Task<bool> SendBookingConfirmationEmailAsync(Booking booking, User user, Event eventItem)
        {
            try
            {
                var subject = $"Booking Confirmation - {eventItem.Name}";
                var body = GenerateBookingConfirmationEmail(user, eventItem, booking);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending booking confirmation email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendBookingCancellationEmailAsync(Booking booking, User user, Event eventItem)
        {
            try
            {
                var subject = $"Booking Cancelled - {eventItem.Name}";
                var body = GenerateBookingCancellationEmail(user, eventItem, booking);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending booking cancellation email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendBookingStatusUpdateEmailAsync(Booking booking, User user, Event eventItem, BookingStatus newStatus)
        {
            try
            {
                var subject = $"Booking Status Update - {eventItem.Name}";
                var body = GenerateBookingStatusUpdateEmail(user, eventItem, booking, newStatus);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending booking status update email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSiteApprovalEmailAsync(User user, HeritageSite site)
        {
            try
            {
                var subject = $"Your Heritage Site Submission Has Been Approved!";
                var body = GenerateSiteApprovalEmail(user, site);
                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending site approval email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSiteRejectionEmailAsync(User user, HeritageSite site, string reason)
        {
            try
            {
                var subject = $"Your Heritage Site Submission Was Rejected";
                var body = GenerateSiteRejectionEmail(user, site, reason);
                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending site rejection email: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Check if email credentials are configured
                if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
                {
                    Console.WriteLine("Email credentials not configured. Skipping email send.");
                    return false;
                }

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                Console.WriteLine($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        private string GenerateBookingConfirmationEmail(User user, Event eventItem, Booking booking)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #D4AF37, #8B0000); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .event-details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #D4AF37; }}
        .booking-id {{ background: #e8f5e8; padding: 10px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Booking Confirmed!</h1>
            <p>Thank you for booking with PenangGo</p>
        </div>
        
        <div class='content'>
            <p>Dear <strong>{user.Name}</strong>,</p>
            
            <p>Your booking has been successfully confirmed! Here are the details:</p>
            
            <div class='booking-id'>
                <strong>Booking ID:</strong> #{booking.Id}
            </div>
            
            <div class='event-details'>
                <h3>📅 Event Details</h3>
                <p><strong>Event:</strong> {eventItem.Name}</p>
                <p><strong>Date:</strong> {eventItem.StartTime:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {eventItem.StartTime:HH:mm} - {eventItem.EndTime:HH:mm}</p>
                <p><strong>Location:</strong> {eventItem.Location}</p>
                <p><strong>Price:</strong> RM {eventItem.Price:F2}</p>
                <p><strong>Description:</strong> {eventItem.Description}</p>
            </div>
            
            <div class='event-details'>
                <h3>📋 Booking Information</h3>
                <p><strong>Booking Date:</strong> {booking.BookingTime:dddd, MMMM dd, yyyy 'at' HH:mm}</p>
                <p><strong>Status:</strong> <span style='color: #28a745; font-weight: bold;'>Confirmed</span></p>
            </div>
            
            <p><strong>Important Notes:</strong></p>
            <ul>
                <li>Please arrive 15 minutes before the event starts</li>
                <li>Bring your booking confirmation (this email)</li>
                <li>Contact us if you need to make any changes</li>
            </ul>
            
            <p>We look forward to seeing you at the event!</p>
            
            <p>Best regards,<br>The PenangGo Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>© 2024 PenangGo - Malaysian Cultural Heritage Sites</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateBookingCancellationEmail(User user, Event eventItem, Booking booking)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545, #721c24); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .event-details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #dc3545; }}
        .booking-id {{ background: #ffe6e6; padding: 10px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Booking Cancelled</h1>
            <p>Your booking has been cancelled</p>
        </div>
        
        <div class='content'>
            <p>Dear <strong>{user.Name}</strong>,</p>
            
            <p>Your booking has been cancelled. Here are the details:</p>
            
            <div class='booking-id'>
                <strong>Booking ID:</strong> #{booking.Id}
            </div>
            
            <div class='event-details'>
                <h3>📅 Event Details</h3>
                <p><strong>Event:</strong> {eventItem.Name}</p>
                <p><strong>Date:</strong> {eventItem.StartTime:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {eventItem.StartTime:HH:mm} - {eventItem.EndTime:HH:mm}</p>
                <p><strong>Location:</strong> {eventItem.Location}</p>
                <p><strong>Price:</strong> RM {eventItem.Price:F2}</p>
            </div>
            
            <div class='event-details'>
                <h3>📋 Cancellation Information</h3>
                <p><strong>Original Booking Date:</strong> {booking.BookingTime:dddd, MMMM dd, yyyy 'at' HH:mm}</p>
                <p><strong>Status:</strong> <span style='color: #dc3545; font-weight: bold;'>Cancelled</span></p>
            </div>
            
            <p>If you have any questions about this cancellation, please contact our support team.</p>
            
            <p>We hope to see you at future events!</p>
            
            <p>Best regards,<br>The PenangGo Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>© 2024 PenangGo - Malaysian Cultural Heritage Sites</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateBookingStatusUpdateEmail(User user, Event eventItem, Booking booking, BookingStatus newStatus)
        {
            var statusText = newStatus switch
            {
                BookingStatus.Confirmed => "Confirmed",
                BookingStatus.Pending => "Pending",
                BookingStatus.Cancelled => "Cancelled",
                BookingStatus.Completed => "Completed",
                _ => "Updated"
            };

            var statusColor = newStatus switch
            {
                BookingStatus.Confirmed => "#28a745",
                BookingStatus.Pending => "#ffc107",
                BookingStatus.Cancelled => "#dc3545",
                BookingStatus.Completed => "#17a2b8",
                _ => "#6c757d"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .event-details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #007bff; }}
        .booking-id {{ background: #e7f3ff; padding: 10px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📝 Booking Status Update</h1>
            <p>Your booking status has been updated</p>
        </div>
        
        <div class='content'>
            <p>Dear <strong>{user.Name}</strong>,</p>
            
            <p>Your booking status has been updated. Here are the details:</p>
            
            <div class='booking-id'>
                <strong>Booking ID:</strong> #{booking.Id}
            </div>
            
            <div class='event-details'>
                <h3>📅 Event Details</h3>
                <p><strong>Event:</strong> {eventItem.Name}</p>
                <p><strong>Date:</strong> {eventItem.StartTime:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {eventItem.StartTime:HH:mm} - {eventItem.EndTime:HH:mm}</p>
                <p><strong>Location:</strong> {eventItem.Location}</p>
                <p><strong>Price:</strong> RM {eventItem.Price:F2}</p>
            </div>
            
            <div class='event-details'>
                <h3>📋 Status Update</h3>
                <p><strong>New Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{statusText}</span></p>
                <p><strong>Original Booking Date:</strong> {booking.BookingTime:dddd, MMMM dd, yyyy 'at' HH:mm}</p>
            </div>
            
            <p>If you have any questions about this status update, please contact our support team.</p>
            
            <p>Best regards,<br>The PenangGo Team</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>© 2024 PenangGo - Malaysian Cultural Heritage Sites</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateSiteApprovalEmail(User user, HeritageSite site)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981, #059669); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .site-details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #10b981; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Submission Approved!</h1>
            <p>Your heritage site is now live on PenangGo</p>
        </div>
        <div class='content'>
            <p>Dear <strong>{user.Name}</strong>,</p>
            <p>Congratulations! Your heritage site submission has been <span style='color: #10b981; font-weight: bold;'>approved</span> by our admin team. Thank you for contributing to the preservation of Malaysian cultural heritage.</p>
            <div class='site-details'>
                <h3>🏛️ Site Details</h3>
                <p><strong>Name:</strong> {site.Name}</p>
                <p><strong>Location:</strong> {site.Location}</p>
                <p><strong>Category:</strong> {site.Category}</p>
                <p><strong>Description:</strong> {site.Description}</p>
            </div>
            <p>Your site is now visible to all users on the PenangGo platform.</p>
            <p>Thank you for your valuable contribution!</p>
            <p>Best regards,<br>The PenangGo Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>© 2024 PenangGo - Malaysian Cultural Heritage Sites</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateSiteRejectionEmail(User user, HeritageSite site, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ef4444, #dc2626); color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .site-details {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #ef4444; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Submission Rejected</h1>
            <p>Your heritage site submission was not approved</p>
        </div>
        <div class='content'>
            <p>Dear <strong>{user.Name}</strong>,</p>
            <p>We regret to inform you that your heritage site submission was <span style='color: #ef4444; font-weight: bold;'>rejected</span> by our admin team. Please see the reason below:</p>
            <div class='site-details'>
                <h3>🏛️ Site Details</h3>
                <p><strong>Name:</strong> {site.Name}</p>
                <p><strong>Location:</strong> {site.Location}</p>
                <p><strong>Category:</strong> {site.Category}</p>
                <p><strong>Description:</strong> {site.Description}</p>
            </div>
            <div class='site-details'>
                <h3>📝 Rejection Reason</h3>
                <p>{reason}</p>
            </div>
            <p>You are welcome to revise your submission and try again. Thank you for your interest in preserving Malaysian cultural heritage.</p>
            <p>Best regards,<br>The PenangGo Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>© 2024 PenangGo - Malaysian Cultural Heritage Sites</p>
        </div>
    </div>
</body>
</html>";
        }
    }
} 