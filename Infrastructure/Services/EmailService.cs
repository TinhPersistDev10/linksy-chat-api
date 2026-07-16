using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;

namespace linksy_backend_api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
                "email_verification" => "Cảm ơn bạn đã đăng ký Linksy Chat! Vui lòng sử dụng mã OTP dưới đây để xác thực email của bạn:",
                "password_reset" => "Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây:",
                "login" => "Đây là mã OTP để đăng nhập vào tài khoản của bạn:",
                _ => "Đây là mã OTP của bạn:"
            };

            var purposeLabel = purpose switch
            {
                "email_verification" => "Xác thực email",
                "password_reset" => "Đặt lại mật khẩu",
                "login" => "Đăng nhập tài khoản",
                _ => "Xác thực tài khoản"
            };

            var otpDigits = string.Join("", otp.Select(c =>
                $"<span style='display:inline-block;width:44px;height:52px;background:#fff;border:2px solid #378ADD;border-radius:8px;font-size:26px;font-weight:700;color:#185FA5;line-height:52px;text-align:center;margin:0 2px;'>{c}</span>"
            ));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Linksy Chat - Xác thực OTP</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f0f4f8;
            font-family: Arial, sans-serif;
        }}
        .wrapper {{
            padding: 40px 20px;
        }}
        .container {{
            max-width: 560px;
            margin: 0 auto;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 24px rgba(24,95,165,0.10);
        }}
        .header {{
            background: linear-gradient(135deg, #185FA5 0%, #378ADD 100%);
            padding: 32px 40px;
            text-align: center;
        }}
        .header-logo {{
            display: inline-block;
            width: 36px;
            height: 36px;
            background: rgba(255,255,255,0.2);
            border-radius: 8px;
            text-align: center;
            line-height: 36px;
            font-size: 20px;
            vertical-align: middle;
            margin-right: 10px;
        }}
        .header-title {{
            color: #fff;
            font-size: 22px;
            font-weight: 700;
            letter-spacing: 0.5px;
            vertical-align: middle;
        }}
        .header-subtitle {{
            color: rgba(255,255,255,0.75);
            font-size: 13px;
            margin: 6px 0 0;
        }}
        .content {{
            background-color: #ffffff;
            padding: 36px 40px;
        }}
        .greeting-block {{
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 24px;
        }}
        .avatar {{
            width: 44px;
            height: 44px;
            border-radius: 50%;
            background: #E6F1FB;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            font-size: 20px;
            flex-shrink: 0;
            vertical-align: middle;
            margin-right: 12px;
        }}
        .greeting-name {{
            margin: 0;
            font-size: 17px;
            font-weight: 600;
            color: #0C447C;
        }}
        .greeting-label {{
            margin: 0;
            font-size: 13px;
            color: #888780;
        }}
        .message-text {{
            color: #444441;
            font-size: 14px;
            line-height: 1.7;
            margin: 0 0 24px;
        }}
        .otp-box {{
            background: #E6F1FB;
            border: 1.5px solid #B5D4F4;
            border-radius: 12px;
            padding: 28px 24px;
            text-align: center;
            margin-bottom: 24px;
        }}
        .otp-label {{
            margin: 0 0 12px;
            font-size: 12px;
            color: #185FA5;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            font-weight: 600;
        }}
        .otp-digits {{
            margin-bottom: 14px;
        }}
        .otp-timer {{
            display: inline-block;
            background: #fff;
            border: 1px solid #B5D4F4;
            border-radius: 20px;
            padding: 5px 14px;
            font-size: 12px;
            color: #185FA5;
            font-weight: 500;
        }}
        .warning-box {{
            background: #E6F1FB;
            border-left: 3px solid #378ADD;
            border-radius: 0 8px 8px 0;
            padding: 14px 16px;
            margin-bottom: 24px;
        }}
        .warning-title {{
            font-size: 13px;
            font-weight: 600;
            color: #0C447C;
            margin: 0 0 8px;
        }}
        .warning-list {{
            margin: 0;
            padding-left: 18px;
            font-size: 13px;
            color: #185FA5;
            line-height: 2;
        }}
        .divider {{
            border: none;
            border-top: 0.5px solid #B5D4F4;
            margin: 0 0 20px;
        }}
        .support-text {{
            margin: 0 0 4px;
            font-size: 13px;
            color: #5F5E5A;
        }}
        .support-link {{
            color: #185FA5;
            text-decoration: none;
            font-weight: 600;
        }}
        .sign-off {{
            font-size: 13px;
            color: #5F5E5A;
            margin: 16px 0 0;
        }}
        .sign-off strong {{
            color: #0C447C;
        }}
        .footer {{
            background: #E6F1FB;
            padding: 18px 40px;
            text-align: center;
            border-top: 0.5px solid #B5D4F4;
        }}
        .footer p {{
            margin: 0 0 4px;
            font-size: 12px;
            color: #378ADD;
        }}
        .footer p:last-child {{
            font-size: 11px;
            color: #85B7EB;
            margin: 0;
        }}
    </style>
</head>
<body>
    <div class='wrapper'>
        <div class='container'>

            <!-- Header -->
            <div class='header'>
                <div>
                    <span class='header-logo'>&#x1F4AC;</span>
                    <span class='header-title'>Linksy Chat</span>
                </div>
                <p class='header-subtitle'>Hệ thống xác thực bảo mật</p>
            </div>

            <!-- Content -->
            <div class='content'>

                <!-- Greeting -->
                <div style='margin-bottom:24px;'>
                    <span class='avatar'>&#x1F464;</span>
                    <span style='vertical-align:middle;'>
                        <p class='greeting-name'>Xin chào, {username}!</p>
                        <p class='greeting-label'>{purposeLabel}</p>
                    </span>
                </div>

                <p class='message-text'>{message}</p>

                <!-- OTP Box -->
                <div class='otp-box'>
                    <p class='otp-label'>Mã xác thực OTP</p>
                    <div class='otp-digits'>
                        {otpDigits}
                    </div>
                    <span class='otp-timer'>&#x23F1; Hiệu lực trong <strong>15 phút</strong></span>
                </div>

                <!-- Warning -->
                <div class='warning-box'>
                    <p class='warning-title'>&#x1F6E1; Lưu ý bảo mật</p>
                    <ul class='warning-list'>
                        <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
                        <li>Mã OTP chỉ được sử dụng một lần duy nhất</li>
                        <li>Nếu bạn không yêu cầu, hãy bỏ qua email này</li>
                    </ul>
                </div>

                <hr class='divider' />

                <p class='support-text'>
                    &#x1F4AC; Cần hỗ trợ? Liên hệ
                    <a href='mailto:support@linksy.com' class='support-link'>support@linksy.com</a>
                </p>

                <p class='sign-off'>
                    Trân trọng,<br>
                    <strong>Đội ngũ Linksy Chat</strong>
                </p>
            </div>

            <!-- Footer -->
            <div class='footer'>
                <p>© 2024 Linksy Chat. All rights reserved.</p>
                <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            </div>

        </div>
    </div>
</body>
</html>";
        }
        //         private object GetOtpEmailTemplate(string username, string otp, string purpose)
        //         {
        //             var message = purpose switch
        //             {
        //                 "email_verification" => "Cảm ơn bạn đã đăng ký Linksy Chat! Vui lòng sử dụng mã OTP dưới đây để xác thực email của bạn:",
        //                 "password_reset" => "Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây:",
        //                 "login" => "Đây là mã OTP để đăng nhập vào tài khoản của bạn:",
        //                 _ => "Đây là mã OTP của bạn:"
        //             };

        //             return $@"
        // <!DOCTYPE html>
        // <html>
        // <head>
        //     <meta charset='utf-8'>
        //     <style>

        //         body {{
        //             font-family: Arial, sans-serif;
        //             line-height: 1.6;
        //             color: #333;
        //         }}
        //         .container {{
        //             max-width: 600px;
        //             margin: 0 auto;
        //             padding: 20px;
        //             background-color: #f9f9f9;
        //         }}
        //         .header {{
        //             background-color: #4CAF50;
        //             color: white;
        //             padding: 20px;
        //             text-align: center;
        //             border-radius: 5px 5px 0 0;
        //         }}
        //         .content {{
        //             background-color: white;
        //             padding: 30px;
        //             border-radius: 0 0 5px 5px;
        //         }}
        //         .otp-box {{
        //             background-color: #f0f0f0;
        //             border: 2px dashed #4CAF50;
        //             padding: 20px;
        //             margin: 20px 0;
        //             text-align: center;
        //             border-radius: 5px;
        //         }}
        //         .otp-code {{
        //             font-size: 32px;
        //             font-weight: bold;
        //             color: #4CAF50;
        //             letter-spacing: 5px;
        //         }}
        //         .footer {{
        //             margin-top: 20px;
        //             text-align: center;
        //             color: #666;
        //             font-size: 12px;
        //         }}
        //         .warning {{
        //             background-color: #fff3cd;
        //             border-left: 4px solid #ffc107;
        //             padding: 10px;
        //             margin: 20px 0;
        //         }}
        //     </style>
        // </head>
        // <body>
        //     <div class='container'>
        //         <div class='header'>
        //             <h1>Linksy Chat</h1>
        //         </div>
        //         <div class='content'>
        //             <h2>Xin chào {username}!</h2>
        //             <p>{message}</p>

        //             <div class='otp-box'>
        //                 <p style='margin: 0; font-size: 14px; color: #666;'>Mã OTP của bạn là:</p>
        //                 <div class='otp-code'>{otp}</div>
        //                 <p style='margin: 10px 0 0 0; font-size: 12px; color: #666;'>Mã này có hiệu lực trong 15 phút</p>
        //             </div>

        //             <div class='warning'>
        //                 <strong>⚠️ Lưu ý:</strong>
        //                 <ul style='margin: 5px 0; padding-left: 20px;'>
        //                     <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
        //                     <li>Mã OTP chỉ được sử dụng một lần</li>
        //                     <li>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email</li>
        //                 </ul>
        //             </div>

        //             <p>Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: support@linksy.com</p>

        //             <p>Trân trọng,<br>Đội ngũ Linksy Chat</p>
        //         </div>
        //         <div class='footer'>
        //             <p>© 2024 Linksy Chat. All rights reserved.</p>
        //             <p>Email này được gửi tự động, vui lòng không trả lời.</p>
        //         </div>
        //     </div>
        // </body>
        // </html>";
        //         }

        private async Task SendEmailAsync(string toEmail, string subject, object body)
        {
            try
            {
                var brevoApiKey = _configuration["Brevo:ApiKey"];
                if (!string.IsNullOrWhiteSpace(brevoApiKey))
                {
                    await SendEmailWithBrevoAsync(toEmail, subject, body.ToString() ?? string.Empty, brevoApiKey);
                    return;
                }

                // Gmail SMTP is intentionally disabled. Use Brevo__ApiKey/Brevo__FromEmail/Brevo__FromName instead.
                throw new InvalidOperationException("Brevo email provider is not configured");

                /*
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
                if (string.IsNullOrEmpty(FromEmail))
                    throw new InvalidOperationException("FromEmail is not configured");
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
                */
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw new Exception("Không thể gửi email. Vui lòng thử lại sau.");
            }
        }

        private async Task SendEmailWithBrevoAsync(string toEmail, string subject, string htmlBody, string apiKey)
        {
            var fromEmail = _configuration["Brevo:FromEmail"] ?? _configuration["Email:FromEmail"];
            var fromName = _configuration["Brevo:FromName"] ?? _configuration["Email:FromName"] ?? "Linksy Chat";

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("Brevo FromEmail is not configured");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                sender = new
                {
                    name = fromName,
                    email = fromEmail
                },
                to = new[]
                {
                    new
                    {
                        email = toEmail
                    }
                },
                subject,
                htmlContent = htmlBody
            });

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo failed. StatusCode={StatusCode}, Body={Body}", response.StatusCode, responseBody);
                throw new Exception("Không thể gửi email. Vui lòng thử lại sau.");
            }

            _logger.LogInformation("Email sent successfully to {ToEmail} via Brevo", toEmail);
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
            <h1>Chào mừng đến với Linksy Chat!</h1>
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
            
            <p>Chúc bạn có trải nghiệm tuyệt vời!<br>Đội ngũ Linksy Chat</p>
        </div>
        <div style='text-align: center; margin-top: 20px; color: #666; font-size: 12px;'>
            <p>© 2024 Linksy Chat. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

    }
}
