using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IEmailService
    {
        Task SendOtpAsync(string email, string otp);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

}
