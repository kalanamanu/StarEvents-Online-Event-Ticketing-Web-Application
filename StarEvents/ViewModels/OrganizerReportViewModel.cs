using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class OrganizerReportViewModel
    {
        public List<OrganizerReportRow> Rows { get; set; }
        public int TotalOrganizers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalEvents { get; set; }
        // Optionally, add filters (date range, organizer, etc.)
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string Search { get; set; }
    }

    public class OrganizerReportRow
    {
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; }
        public string Email { get; set; }
        public int EventCount { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AvgEventRating { get; set; }
        public int PartnershipEvents { get; set; }
        public double RevenueContributionPct { get; set; }
        public double AvgEventsPerMonth { get; set; }
        public double AvgTicketsPerEvent { get; set; }
        public double SelloutRatePct { get; set; }
    }
}