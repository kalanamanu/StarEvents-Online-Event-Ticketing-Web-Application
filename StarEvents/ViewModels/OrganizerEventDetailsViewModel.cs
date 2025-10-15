using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class OrganizerEventDetailsViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string VenueName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public List<SeatCategoryInfo> SeatCategories { get; set; }
        public List<BookingInfo> Bookings { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public List<PromotionSeatDiscountViewModel> Promotions { get; set; } 
    }

    public class SeatCategoryInfo
    {
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
    }

    public class BookingInfo
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int Quantity { get; set; }
        public DateTime BookedAt { get; set; }
        public string Status { get; set; }
    }

    public class PromotionSeatDiscountViewModel
    {
        public string DiscountName { get; set; }
        public string SeatCategory { get; set; }
        public string DiscountType { get; set; } 
        public double? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal BasePrice { get; set; }
        public decimal DiscountedPrice { get; set; }
    }
}