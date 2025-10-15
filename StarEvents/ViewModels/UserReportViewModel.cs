using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class UserReportViewModel
    {
        public List<UserReportRow> Rows { get; set; }
        public int TotalUsers { get; set; }
        public int TotalActive { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrganizers { get; set; }
        // Filters
        public string Search { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<string> RoleList { get; set; }
        public List<string> StatusList { get; set; }
        public List<RegistrationTrendPoint> RegistrationTrends { get; set; }
        public int TotalLoyaltyPoints { get; set; }
        public double AvgLoyaltyPoints { get; set; }
        public List<LoyaltyUserSummary> TopLoyaltyCustomers { get; set; }
        public List<CustomerSegmentSummary> CustomerSegments { get; set; }
        public List<UserBehaviorSummary> TopUserActivities { get; set; }
        public double AvgBookingsPerUser { get; set; }
        public Dictionary<string, double> RetentionRates { get; set; }
    }

    public class UserReportRow
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
    }
}