using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;

namespace linksy_backend_api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        async Task IEmailService.SendOtpEmailAsync(string toEmail, string username, string otp, string purpose)
        {
            var subject = purpose switch
            {
                "email_verification" => "Xác thực email",
                "password_reset" => "Đặt lại mật khẩu",
                "login" => "Đăng nhập",
                _ => "OTP Code - Linksy Chat"
            };
            var body = GetOtpEmailTemplate(username, otp, purpose);
            await SendEmailAsync(toEmail, subject, body);
        }

        private object GetOtpEmailTemplate(string username, string otp, string purpose)
        {
            var message = purpose switch
            {
                "email_verification" => "Cảm ơn bạn đã đăng ký ChatApp! Vui lòng sử dụng mã OTP dưới đây để xác thực email của bạn:",
                "password_reset" => "Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây:",
                "login" => "Đây là mã OTP để đăng nhập vào tài khoản của bạn:",
                _ => "Đây là mã OTP của bạn:"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background-color: #4CAF50;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .otp-box {{
            background-color: #f0f0f0;
            border: 2px dashed #4CAF50;
            padding: 20px;
            margin: 20px 0;
            text-align: center;
            border-radius: 5px;
        }}
        .otp-code {{
            font-size: 32px;
            font-weight: bold;
            color: #4CAF50;
            letter-spacing: 5px;
        }}
        .footer {{
            margin-top: 20px;
            text-align: center;
            color: #666;
            font-size: 12px;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ChatApp</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {username}!</h2>
            <p>{message}</p>
            
            <div class='otp-box'>
                <p style='margin: 0; font-size: 14px; color: #666;'>Mã OTP của bạn là:</p>
                <div class='otp-code'>{otp}</div>
                <p style='margin: 10px 0 0 0; font-size: 12px; color: #666;'>Mã này có hiệu lực trong 15 phút</p>
            </div>

            <div class='warning'>
                <strong>⚠️ Lưu ý:</strong>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
                    <li>Mã OTP chỉ được sử dụng một lần</li>
                    <li>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email</li>
                </ul>
            </div>

            <p>Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: support@chatapp.com</p>
            
            <p>Trân trọng,<br>Đội ngũ ChatApp</p>
        </div>
        <div class='footer'>
            <p>© 2024 ChatApp. All rights reserved.</p>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
        </div>
    </div>
</body>
</html>";
        }
        private async Task SendEmailAsync(string toEmail, string subject, object body)
        {
            try
            {

                var SmtpHost = _configuration["Email:SmtpHost"];
                var SmtpPort = int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587;
                var SmtpUsername = _configuration["Email:SmtpUsername"];
                var SmtpPassword = _configuration["Email:SmtpPassword"];
                var FromEmail = _configuration["Email:FromEmail"];
                var FromName = _configuration["Email:FromName"];

                // Tạo SMTP client
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                    EnableSsl = true
                };
                var mailMessage = new MailMessage();
                if (!string.IsNullOrEmpty(FromEmail))
                {
                    // Tạo email message
                    mailMessage = new MailMessage
                    {

                        From = new MailAddress(FromEmail, FromName),
                        Subject = subject,
                        Body = body.ToString(),
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);
                }
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw new Exception("Không thể gửi email. Vui lòng thử lại sau.");
            }
        }

        async Task IEmailService.SendWelcomeEmailAsync(string toEmail, string username)
        {
            var subject = "Chào mừng đến với Linksy! ";
            var body = GetWelcomeEmailTemplate(username);

            await SendEmailAsync(toEmail, subject, body);
        }

        private object GetWelcomeEmailTemplate(string username)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: white;
            padding: 30px;
            border-radius: 0 0 5px 5px;
        }}
        .feature {{
            background-color: #f8f9fa;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin: 15px 0;
            border-radius: 3px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background-color: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Chào mừng đến với ChatApp!</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {username}!</h2>
            <p>Cảm ơn bạn đã tham gia cộng đồng Linksy. Chúng tôi rất vui được có bạn!</p>
            
            <h3>Một số tính năng tuyệt vời bạn có thể khám phá:</h3>
            
            <div class='feature'>
                <strong>Chat 1-1 và Nhóm</strong>
                <p>Kết nối với bạn bè và tạo nhóm chat để làm việc hiệu quả hơn.</p>
            </div>
            
            <div class='feature'>
                <strong>Thông báo Real-time</strong>
                <p>Nhận thông báo ngay lập tức khi có tin nhắn mới.</p>
            </div>
            
            <div class='feature'>
                <strong>Bảo mật cao</strong>
                <p>Tin nhắn của bạn được mã hóa và bảo mật tuyệt đối.</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='#' class='button'>Bắt đầu Chat ngay</a>
            </div>
            
            <p>Nếu có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với chúng tôi!</p>
            
            <p>Chúc bạn có trải nghiệm tuyệt vời!<br>Đội ngũ ChatApp</p>
        </div>
        <div style='text-align: center; margin-top: 20px; color: #666; font-size: 12px;'>
            <p>© 2024 ChatApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

    }
}