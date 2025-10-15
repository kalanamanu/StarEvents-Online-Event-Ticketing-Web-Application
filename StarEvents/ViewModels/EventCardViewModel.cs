using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class EventCardViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public decimal StartingPrice { get; set; }

        public string VenueName { get; set; }

        public List<PromotionViewModel> Promotions { get; set; }
    }

}