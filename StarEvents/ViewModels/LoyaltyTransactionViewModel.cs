using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class LoyaltyTransactionViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }         // "Earn", "Redeem", etc.
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int Points { get; set; }
        public int Balance { get; set; }
    }

    public class LoyaltyViewModel
    {
        public int CurrentPoints { get; set; }
        public List<LoyaltyTransactionViewModel> Transactions { get; set; }
    }
}