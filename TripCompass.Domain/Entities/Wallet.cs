using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class Wallet
    {
        public long WalletId { get; set; }
        public long UserId { get; set; }
        public int Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
