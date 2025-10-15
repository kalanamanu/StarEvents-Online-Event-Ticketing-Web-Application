using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class CheckoutViewModel
    {
        // Order summary
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }
        public string SeatCategoryName { get; set; }
        public int SeatCategoryId { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerTicket { get; set; }
        public decimal? OriginalPricePerTicket { get; set; }
        public decimal TotalPrice { get; set; }
        public string PromotionLabel { get; set; }

        // Loyalty
        public int AvailableLoyaltyPoints { get; set; }
        public int PointsToRedeem { get; set; }

        // Cards
        public List<CardSummary> SavedCards { get; set; }
        // ... add more as needed
    }
    public class CardSummary
    {
        public int CardId { get; set; }
        public string CardNumber { get; set; }
        public string CardHolderName { get; set; }
        public string Expiry { get; set; }
        public bool IsDefault { get; set; }
    }
}