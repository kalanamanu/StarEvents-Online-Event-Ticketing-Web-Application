using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class EventAnalyticsViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal TotalPotentialDiscount { get; set; }
        public decimal DiscountedPotentialRevenue { get; set; }
        public List<TicketCategoryAnalytics> CategoryBreakdown { get; set; }
        public List<TicketSalesTrend> SalesTrend { get; set; }

        public List<EventDiscountSummary> Discounts { get; set; }

       
    }

    public class EventDiscountSummary
    {
        public string DiscountName { get; set; }
        public string DiscountType { get; set; }
        public double? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string SeatCategory { get; set; }
        public int? MaxUsage { get; set; }

    }
}