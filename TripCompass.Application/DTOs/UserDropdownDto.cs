using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.DTOs
{
    public class UserDropdownDto
    {
        public string UserName { get; set; } = "";
        public string AvatarUrl { get; set; } = "";

        public int ReputationLevel { get; set; }
        public int ReputationScore { get; set; }

        public int WalletBalance { get; set; }
    }
}
