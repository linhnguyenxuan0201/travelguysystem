using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class EmailOtp
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
