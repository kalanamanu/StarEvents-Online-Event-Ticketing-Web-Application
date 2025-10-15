using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize]
    public class CustomerDiscountsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /CustomerDiscounts/DiscountedEvents
        public ActionResult DiscountedEvents()
        {
            DateTime now = DateTime.Now;

            // Get all events with at least one active, date-valid EventDiscount
            var discountedEvents = db.Events
                .Where(e => e.EventDiscounts.Any(d =>
                    d.IsActive &&
                    d.StartDate <= now &&
                    d.EndDate >= now
                ))
                .ToList();

            // Map to your EventCardViewModel, ensuring Label and Description match main Events page
            var eventCards = discountedEvents.Select(e => new EventCardViewModel
            {
                EventId = e.EventId,
                Title = e.Title,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                ImageUrl = e.ImageUrl,
                Category = e.Category,
                VenueName = e.Venue != null ? e.Venue.VenueName : "",
                StartingPrice = e.SeatCategories.Any() ? e.SeatCategories.Min(sc => sc.Price) : 0,
                Promotions = e.EventDiscounts
        .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now)
        .Select(d => new PromotionViewModel
        {
            PromotionId = d.DiscountId,
            Title = d.DiscountName,
            DiscountValue = (decimal)(d.DiscountPercent > 0 ? d.DiscountPercent : d.DiscountAmount),
            DiscountType = d.DiscountPercent > 0 ? "Percent" : "Amount",
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            IsActive = d.IsActive,
            EventTitle = e.Title,
            // Compose the label like the main events page (with emoji, name, and value)
            Label = d.DiscountPercent > 0
                ? $"🔥 {d.DiscountName}: {d.DiscountPercent:0.##}% off"
                : d.DiscountAmount > 0
                    ? $"🔥 {d.DiscountName}: {d.DiscountAmount:N0} off"
                    : $"🔥 {d.DiscountName}",
            Type = "Discount",
            
            Description = string.IsNullOrEmpty(d.Description)
                ? $"Valid until {d.EndDate:MMM dd}"
                : $"Valid until {d.EndDate:MMM dd}",
            Color = "#10b981"
        }).ToList()
            }).ToList();

            return View(eventCards);
        }
    }
}