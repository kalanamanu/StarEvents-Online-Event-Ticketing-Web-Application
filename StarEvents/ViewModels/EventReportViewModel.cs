using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class EventReportViewModel
    {
        public List<EventReportRow> Rows { get; set; }
        public int TotalEvents { get; set; }
        public int TotalTickets { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        public string Search { get; set; }
        public string Status { get; set; }
        public string Organizer { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<string> OrganizerList { get; set; }
        public List<string> StatusList { get; set; }
        public double AvgSelloutRatePct { get; set; }
        public decimal AvgTicketPrice { get; set; }
        // ...your existing filter properties...
        public List<OrganizerEffectivenessSummary> OrganizerSummaries { get; set; }
    }

    public class EventReportRow
    {
        public string EventTitle { get; set; }
        public string Category { get; set; }
        public string Organizer { get; set; }
        public DateTime? EventDate { get; set; }
        public string Venue { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public double SelloutRatePct { get; set; }
        public decimal AvgTicketPrice { get; set; }
        public string Status { get; set; }
    }

    public class OrganizerEffectivenessSummary
    {
        public string Organizer { get; set; }
        public int EventCount { get; set; }
        public int TotalTickets { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AvgTicketsPerEvent { get; set; }
        public decimal AvgRevenuePerEvent { get; set; }
    }
}