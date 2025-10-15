using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class TicketSalesTrend
    {
        public DateTime Date { get; set; }
        public int Sold { get; set; }
        public decimal Revenue { get; set; }
    }
}