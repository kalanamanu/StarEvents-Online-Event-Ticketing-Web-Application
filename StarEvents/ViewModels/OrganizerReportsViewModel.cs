using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class OrganizerReportsViewModel
    {
        // Summary cards
        public int TotalEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscountGiven { get; set; }

        // Charts data
        public List<SalesTrendPoint> SalesTrend { get; set; }
        public List<EventSummary> Events { get; set; }
        public List<DiscountUsageSummary> DiscountUsages { get; set; }
        public List<TopEventSummary> TopEvents { get; set; }

        // Filters (optional)
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    // For line chart over time
    public class SalesTrendPoint
    {
        public DateTime Date { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    // For main event table
    public class EventSummary
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public int TicketsSold { get; set; }
        public int TotalSeats { get; set; }
        public decimal Revenue { get; set; }
        public decimal DiscountGiven { get; set; }
        public string Status { get; set; }
    }

    // For discount usage stats
    public class DiscountUsageSummary
    {
        public string DiscountName { get; set; }
        public int TimesUsed { get; set; }
        public decimal TotalDiscountGiven { get; set; }
    }

    // For top events by revenue/tickets
    public class TopEventSummary
    {
        public string EventTitle { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}