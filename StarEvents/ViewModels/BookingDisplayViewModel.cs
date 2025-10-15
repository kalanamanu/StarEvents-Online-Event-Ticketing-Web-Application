using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class BookingDisplayViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }
        public DateTime BookingDate { get; set; }
        public int Quantity { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentReference { get; set; }
    }
    public class MyBookingsViewModel
    {
        public List<BookingDisplayViewModel> Bookings { get; set; }
    }
}