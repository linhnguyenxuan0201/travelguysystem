using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using TripCompass.Application.Interfaces.Repositories;

namespace TripCompass.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private static SmtpClient CreateSmtp()
        {
            return new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(
                    "linhnguyenxuan0201@gmail.com",
                    "jekeumyusojegxvu"
                ),
                EnableSsl = true
            };
        }

        private static MailAddress FromAddress =>
            new MailAddress("linhnguyenxuan0201@gmail.com", "TripCompass");

        public async Task SendOtpAsync(string email, string otp)
        {
            var message = new MailMessage();
            message.To.Add(email);
            message.Subject = "TripCompass - Verify your email";
            message.Body = $"""
        Your OTP code is: {otp}
        This code will expire in 5 minutes.
        """;

            message.From = FromAddress;

            using var smtp = CreateSmtp();

            await smtp.SendMailAsync(message);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MailMessage();
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.From = FromAddress;

            using var smtp = CreateSmtp();
            await smtp.SendMailAsync(message);
        }
    }
}
