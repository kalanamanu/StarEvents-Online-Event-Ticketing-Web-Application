using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class TicketCategoryAnalytics
    {
        public string Category { get; set; }
        public int Sold { get; set; }
        public int TotalSeats { get; set; }
        public decimal Price { get; set; }
        public decimal Revenue { get; set; }
        public decimal MaxRevenue { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int MaxDiscountUsage { get; set; }
        public decimal PotentialDiscount { get; set; }
    }
}