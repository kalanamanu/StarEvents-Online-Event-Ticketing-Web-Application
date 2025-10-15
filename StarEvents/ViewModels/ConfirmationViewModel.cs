using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class ConfirmationViewModel
    {
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }
        public string SeatCategory { get; set; }
        public string CustomerName { get; set; }
        public List<ConfirmationTicket> Tickets { get; set; }
        public string BookingCode { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string PaymentReference { get; set; }
    }

    public class ConfirmationTicket
    {
        public int TicketNumber { get; set; }     
        public string QRCodeUrl { get; set; }
        public string TicketCode { get; set; }
    }
}