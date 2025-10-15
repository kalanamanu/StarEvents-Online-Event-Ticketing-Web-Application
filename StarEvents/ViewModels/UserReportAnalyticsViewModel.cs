using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class UserReportAnalyticsViewModel
    {
        public List<UserReportRow> Rows { get; set; }
        public int TotalUsers { get; set; }
        public int TotalActive { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrganizers { get; set; }

        // Registration trends
        public List<RegistrationTrendPoint> RegistrationTrends { get; set; }

        // Loyalty
        public int TotalLoyaltyPoints { get; set; }
        public double AvgLoyaltyPoints { get; set; }
        public List<LoyaltyUserSummary> TopLoyaltyCustomers { get; set; }

        // Segmentation
        public List<CustomerSegmentSummary> CustomerSegments { get; set; }

        // Behavior
        public List<UserBehaviorSummary> TopUserActivities { get; set; }
        public double AvgBookingsPerUser { get; set; }
        public Dictionary<string, double> RetentionRates { get; set; }
        public List<string> StatusList { get; internal set; }
        public string Search { get; internal set; }
        public string Status { get; internal set; }
        public string Role { get; internal set; }
        public DateTime? From { get; internal set; }
        public DateTime? To { get; internal set; }
        public List<string> RoleList { get; internal set; }
    }

    public class RegistrationTrendPoint
    {
        public string Period { get; set; } // e.g., "2025-10"
        public int Count { get; set; }
    }

    public class LoyaltyUserSummary
    {
        public string Username { get; set; }
        public int Points { get; set; }
    }

    public class CustomerSegmentSummary
    {
        public string SegmentName { get; set; }
        public int UserCount { get; set; }
        public string Description { get; set; }
    }

    public class UserBehaviorSummary
    {
        public string Activity { get; set; }
        public int Count { get; set; }
    }
}