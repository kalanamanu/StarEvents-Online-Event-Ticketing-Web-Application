using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class OrganizerBookingListViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public List<OrganizerBookingItemViewModel> Bookings { get; set; }
    }

    public class OrganizerBookingItemViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } // Active, Canceled
        public decimal TotalAmount { get; set; }
        public string BookingCode { get; set; }
    }
}