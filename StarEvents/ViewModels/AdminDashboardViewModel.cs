using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Key Metrics
        public int TotalUsers { get; set; }
        public int TotalOrganizers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        // Sales Chart Data
        public List<SalesTrendPoint> SalesTrend { get; set; }

        // Activity Logs (latest N)
        public List<ActivityLogEntry> RecentActivities { get; set; }

        public List<TopEventSummary> TopEvents { get; set; } = new List<TopEventSummary>();
        public List<UserGrowthPoint> UserGrowth { get; set; } = new List<UserGrowthPoint>();
    }

    // For system activity feed
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } // e.g. "Booking", "Registration"
        public string Description { get; set; }
        public string PerformedBy { get; set; }
    }

    public class UserGrowthPoint
    {
        public string Month { get; set; } // "2025-10"
        public int Count { get; set; }
    }
}