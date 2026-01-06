using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.DTOs
{
    public class MonthlyStatDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ReviewCount { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
    }

}
