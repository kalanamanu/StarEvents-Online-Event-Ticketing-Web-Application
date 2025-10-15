using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    public class HomeController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // Helper to infer promotion type, icon, and color from discount name
        private string GetPromotionType(string name)
        {
            name = (name ?? "").ToLower();
            if (name.Contains("vip") || name.Contains("premium") || name.Contains("gold"))
                return "vip";
            if (name.Contains("early") || name.Contains("advance") || name.Contains("pre"))
                return "earlybird";
            if (name.Contains("flash") || name.Contains("today") || name.Contains("urgent"))
                return "flashsale";
            if (name.Contains("group") || name.Contains("team"))
                return "group";
            return "discount";
        }

        private (string icon, string color) GetIconAndColor(string type)
        {
            switch (type)
            {
                case "vip": return ("💎", "#8e24aa");
                case "earlybird": return ("🔥", "#388e3c");
                case "flashsale": return ("⚡", "#ff7043");
                case "group": return ("👥", "#1976d2");
                default: return ("🎁", "#ff7043");
            }
        }

        // Helper method to get featured events with starting price from SeatCategories and mapped promotions
        private List<EventCardViewModel> GetFeaturedEvents()
        {
            var featuredEvents = db.Events
                .Include("Venue")
                .Include("EventDiscounts")
                .Where(e => e.IsActive == true && e.IsPublished == true && e.EventDate >= DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(4)
                .ToList()
                .Select(e => new EventCardViewModel
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    Description = e.Description,
                    ImageUrl = e.ImageUrl,
                    Category = e.Category,
                    VenueName = e.Venue != null ? e.Venue.VenueName : "",
                    StartingPrice = e.SeatCategories != null && e.SeatCategories.Any()
                        ? e.SeatCategories.Min(sc => (decimal?)sc.Price) ?? 0
                        : 0,
                    Promotions = e.EventDiscounts != null
                        ? e.EventDiscounts
                            .Where(d => d.IsActive &&
                                        (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                                        (d.EndDate == null || d.EndDate >= DateTime.Now))
                            .Select(d => {
                                var promoType = GetPromotionType(d.DiscountName);
                                var (icon, color) = GetIconAndColor(promoType);
                                // Handle nullable DateTime
                                var startDate = d.StartDate;
                                var endDate = d.EndDate;
                                string description = $"Valid until {endDate:MMM dd}";
                                return new PromotionViewModel
                                {
                                    PromotionId = d.DiscountId,
                                    Title = d.DiscountName,
                                    DiscountValue = d.DiscountPercent ?? 0,
                                    DiscountType = "Percentage", // Or "Fixed" if you have that logic
                                    StartDate = startDate,
                                    EndDate = endDate,
                                    IsActive = d.IsActive,
                                    Label = d.DiscountName + (d.DiscountPercent.HasValue ? $": {d.DiscountPercent}% off" : ""),
                                    Type = promoType,
                                    Icon = icon,
                                    Description = description,
                                    Color = color
                                };
                            }).ToList()
                        : new List<PromotionViewModel>()
                })
                .ToList();

            return featuredEvents;
        }

        public ActionResult Index()
        {
            var model = new HomeViewModel
            {
                FeaturedEvents = GetFeaturedEvents()
            };
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "About";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }

    public class HomeViewModel
    {
        public List<EventCardViewModel> FeaturedEvents { get; set; }
    }
}