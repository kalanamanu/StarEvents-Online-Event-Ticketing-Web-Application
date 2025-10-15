using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class SalesReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Category { get; set; }
        public string EventName { get; set; }
        public string Organizer { get; set; }
        public List<SalesReportRow> Rows { get; set; }

        // Analytics additions
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgTicketPrice { get; set; }
        public List<SalesCategorySummary> ByCategory { get; set; }
        public List<SalesEventSummary> TopEvents { get; set; }
        public List<SalesEventSummary> WorstEvents { get; set; }
    }

    public class SalesReportRow
    {
        public string EventTitle { get; set; }
        public string Category { get; set; }
        public string Organizer { get; set; }
        public DateTime Date { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesCategorySummary
    {
        public string Category { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesEventSummary
    {
        public string EventTitle { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}