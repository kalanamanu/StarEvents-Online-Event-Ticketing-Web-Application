using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    
    
        public class AdminEventDetailsViewModel
        {
            public int EventId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string OrganizerName { get; set; }
            public string OrganizerEmail { get; set; }
            public string Category { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int VenueId { get; set; }
            public string VenueName { get; set; }
            public string Status { get; set; }
            public int TicketsSold { get; set; }
            public int TotalTickets { get; set; }
            public decimal TotalRevenue { get; set; }
            public List<TicketSaleSummary> TicketSales { get; set; } // breakdown by seat type, etc.

            // New additions for richer event details view:
            public string ImageUrl { get; set; }
            public string Location { get; set; }
            public bool IsPublished { get; set; }

            public List<SeatCategorySummary> SeatCategories { get; set; }
            public List<PromotionSummary> Promotions { get; set; }
            public int AvailableSeats { get; set; }
            public List<BookingSummary> Bookings { get; set; }
            public int TotalSeats { get; internal set; }
            public DateTime EventDate { get; internal set; }
        }

        public class TicketSaleSummary
        {
            public string TicketType { get; set; }
            public int QuantitySold { get; set; }
            public decimal Revenue { get; set; }
            public decimal Price { get; set; }
        }

        public class SeatCategorySummary
        {
            public string CategoryName { get; set; }
            public decimal Price { get; set; }
            public int TotalSeats { get; set; }
            public int AvailableSeats { get; set; }
        }

        public class PromotionSummary
        {
            public string DiscountName { get; set; }
            public string SeatCategory { get; set; }
            public string DiscountType { get; set; } // "percent" or "amount"
            public decimal? DiscountPercent { get; set; }
            public decimal? DiscountAmount { get; set; }
            public decimal BasePrice { get; set; }
            public decimal DiscountedPrice { get; set; }
        }

        public class BookingSummary
        {
            public string CustomerName { get; set; }
            public string Email { get; set; }
            public int Quantity { get; set; }
            public string Status { get; set; }
            public DateTime BookedAt { get; set; }
        }
    
}