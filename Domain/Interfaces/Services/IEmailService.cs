using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string username, string otp, string purpose);
        Task SendWelcomeEmailAsync(string toEmail, string username);
        Task SendNotificationEmailAsync(string toEmail, string username, string title, string body);
    }
}