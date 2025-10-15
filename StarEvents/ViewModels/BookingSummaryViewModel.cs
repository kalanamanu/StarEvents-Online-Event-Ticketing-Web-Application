using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class BookingSummaryViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string SeatCategoryName { get; set; }
        public int SeatCategoryId { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerTicket { get; set; }
        public decimal? OriginalPricePerTicket { get; set; }
        public decimal TotalPrice { get; set; }
        public string PromotionLabel { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }

        // Loyalty program integration
        public int AvailableLoyaltyPoints { get; set; }     // Points user can redeem
        public int PointsToRedeem { get; set; }             // Points the user wants to redeem (set at checkout)
        public int PointsEarnedThisBooking { get; set; }    // Points earned for this booking
        public decimal RedeemDiscount => PointsToRedeem;    // LKR discount (1 point = 1 LKR)
        public decimal PayableAfterRedeem => TotalPrice - RedeemDiscount; // New total

        // Optionally, add a flag if points were applied
        public bool LoyaltyPointsApplied => PointsToRedeem > 0;
    }
}