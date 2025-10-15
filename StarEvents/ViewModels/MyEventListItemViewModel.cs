using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class MyEventListItemViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public DateTime EventDate { get; set; }
        public bool IsPublished { get; set; }
        public int PromotionCount { get; set; }
        public string Location { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public string VenueName { get; set; }
        public List<DiscountSummary> Discounts { get; internal set; }
    }

    public class DiscountSummary
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double? Percent { get; set; }
        public decimal? Amount { get; set; }
        public string SeatCategory { get; set; }
        public int? MaxUsage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }


}