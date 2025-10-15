using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class MyTicketsViewModel
    {
        public List<TicketPartialViewModel> Tickets { get; set; }
        public string Search { get; set; }
        public string Filter { get; set; }
        public int UsedCount { get; set; }
        public int ActiveCount { get; set; }
    }
}