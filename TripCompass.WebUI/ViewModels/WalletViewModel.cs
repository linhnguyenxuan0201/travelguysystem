using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels
{
    public class WalletViewModel
    {
        public int Balance { get; set; }
        public List<WalletTransactionItem> Transactions { get; set; } = new();
    }

    public class WalletTransactionItem
    {
        public string Type { get; set; } = "";
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? ReferenceId { get; set; }
    }
}
