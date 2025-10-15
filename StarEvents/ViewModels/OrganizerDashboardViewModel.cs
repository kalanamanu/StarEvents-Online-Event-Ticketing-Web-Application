using System;
using System.Collections.Generic;

namespace StarEvents.ViewModels
{
    public class OrganizerDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public int UnreadNotifications { get; set; }
        public List<OrganizerDashboardEventViewModel> UpcomingEvents { get; set; }
        public List<OrganizerDashboardBookingViewModel> RecentBookings { get; set; }
    }

    public class OrganizerDashboardEventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime? EventDate { get; set; }      // Changed to nullable
    }

    public class OrganizerDashboardBookingViewModel
    {
        public string CustomerName { get; set; }
        public string EventTitle { get; set; }
        public int Quantity { get; set; }
        public DateTime? BookedAt { get; set; }       // Changed to nullable
    }
}