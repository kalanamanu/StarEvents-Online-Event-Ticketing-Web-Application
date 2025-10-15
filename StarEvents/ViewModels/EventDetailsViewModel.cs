using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class EventDetailsViewModel
    {
        // Main Event Info
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string ImageUrl { get; set; }
        public string OrganizerName { get; set; }

        // Venue
        public string VenueName { get; set; }
        public string VenueAddress { get; set; }
        public int? VenueCapacity { get; set; }

        // Dynamic Seat Categories (for the booking panel)
        public List<SeatCategoryViewModel> SeatCategories { get; set; } = new List<SeatCategoryViewModel>();

        // Optional: List of active discounts for this event
        public List<EventDiscountViewModel> ActiveDiscounts { get; set; } = new List<EventDiscountViewModel>();

        // For showing "Starting from $X"
        public decimal StartingPrice
        {
            get
            {
                if (SeatCategories == null || SeatCategories.Count == 0)
                    return 0;
                decimal min = decimal.MaxValue;
                foreach (var cat in SeatCategories)
                {
                    var price = cat.DiscountedPrice ?? cat.Price;
                    if (price < min) min = price;
                }
                return min;
            }
        }
    }

    public class SeatCategoryViewModel
    {
        public int SeatCategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        
        public string DiscountLabel { get; set; } 
    }

}