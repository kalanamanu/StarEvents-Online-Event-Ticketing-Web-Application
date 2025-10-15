using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    public class BookingController : Controller
    {
        [HttpPost]
        public ActionResult Summary(int eventId, int seatCategoryId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            var db = new StarEventsDBEntities();

            // Fetch event, seat category, discounts, and venue
            var ev = db.Events
                .Include("SeatCategories")
                .Include("EventDiscounts")
                .Include("Venue")
                .FirstOrDefault(e => e.EventId == eventId);

            var sc = ev.SeatCategories.FirstOrDefault(s => s.SeatCategoryId == seatCategoryId);

            // Fetch the logged-in user by email (or username, as appropriate)
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            // Get related customer profile for full name, phone, address
            var customerProfile = db.CustomerProfiles.FirstOrDefault(p => p.CustomerId == user.UserId);

            string displayName = customerProfile?.FullName
                                 ?? user?.Username
                                 ?? user?.Email
                                 ?? User.Identity.Name;

            string displayEmail = user?.Email;
            string displayPhone = customerProfile?.PhoneNumber;
            string displayAddress = customerProfile?.Address;

            // Calculate active discount, if any
            var activeDiscount = ev.EventDiscounts
                .Where(d => d.IsActive &&
                            (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                            (d.EndDate == null || d.EndDate >= DateTime.Now))
                .OrderByDescending(d => d.DiscountPercent ?? 0)
                .FirstOrDefault();

            decimal pricePerTicket = sc.Price;
            decimal? discounted = null;
            string promoLabel = null;

            if (activeDiscount != null && activeDiscount.DiscountPercent.HasValue)
            {
                discounted = pricePerTicket - (pricePerTicket * activeDiscount.DiscountPercent.Value / 100m);
                promoLabel = $"{activeDiscount.DiscountName}: {activeDiscount.DiscountPercent.Value:0.#}% off";
                if (activeDiscount.EndDate != null && activeDiscount.EndDate != DateTime.MinValue)
                    promoLabel += $" (until {activeDiscount.EndDate:MMM dd, yyyy})";
            }

            decimal finalPrice = discounted ?? pricePerTicket;
            decimal totalPrice = finalPrice * quantity;

            // -------- Loyalty Points Integration --------
            // Calculate available loyalty points for the user
            int earned = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Earn").Sum(lp => (int?)lp.Amount) ?? 0;
            int redeemed = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Redeem").Sum(lp => (int?)lp.Amount) ?? 0;
            int availablePoints = earned - redeemed;

            // Calculate points earned for this booking (e.g., 1 point per LKR 100 spent)
            int pointsEarnedForThisBooking = (int)(totalPrice / 100);

            var vm = new BookingSummaryViewModel
            {
                EventId = ev.EventId,
                EventTitle = ev.Title,
                EventDate = ev.EventDate,
                VenueName = ev.Venue?.VenueName ?? "",
                Location = ev.Location,
                Category = ev.Category,
                ImageUrl = ev.ImageUrl,
                SeatCategoryName = sc.CategoryName,
                SeatCategoryId = sc.SeatCategoryId,
                Quantity = quantity,
                PricePerTicket = finalPrice,
                OriginalPricePerTicket = (discounted.HasValue && discounted.Value < pricePerTicket) ? (decimal?)pricePerTicket : null,
                TotalPrice = totalPrice,
                PromotionLabel = promoLabel,
                CustomerName = displayName,
                CustomerEmail = displayEmail,
                CustomerPhone = displayPhone,
                CustomerAddress = displayAddress,
                AvailableLoyaltyPoints = availablePoints,
                PointsEarnedThisBooking = pointsEarnedForThisBooking
            };

            // Log the booking summary view
            db.ActivityLogs.Add(new ActivityLog
            {
                Timestamp = DateTime.Now,
                ActivityType = "BookingSummaryViewed",
                Description = $"User '{displayName}' viewed booking summary for event '{ev.Title}' ({quantity} x {sc.CategoryName}).",
                PerformedBy = displayName,
                RelatedEntityId = ev.EventId,
                EntityType = "Event"
            });
            db.SaveChanges();

            return View(vm);
        }


    }
}