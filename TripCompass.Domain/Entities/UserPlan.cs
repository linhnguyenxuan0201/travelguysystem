using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class UserPlan
    {
        public long UserPlanId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; } = null!;

        public string PlanCode { get; set; } = null!; // Free / Pro / Enterprise

        public DateTime StartedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }

}
