using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class AdminEventListViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string OrganizerName { get; set; }
        public string VenueName { get; set; }
        public DateTime EventDate { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public string Status { get; set; }
        public int TotalTickets { get; set; }
    }
}