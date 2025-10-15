using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class OrganizerBookingDetailsViewModel
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }

        public string BookingCode { get; set; }
        public int TicketCount { get; set; }

        public List<OrganizerTicketViewModel> Tickets { get; set; }
    }

    public class OrganizerTicketViewModel
    {
        public int TicketId { get; set; }
        public string Category { get; set; }
        public string Seat { get; set; }
        public bool IsUsed { get; set; }

        public string TicketCode { get; set; }
    }
}