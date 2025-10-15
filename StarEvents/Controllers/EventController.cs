using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;
using System.Data.Entity;

namespace StarEvents.Controllers
{
    public class EventsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // Helpers for promo mapping
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

        // GET: /Events
        public ActionResult Index()
        {
            var events = db.Events
                .Include(e => e.Venue)
                .Include(e => e.EventDiscounts)
                .Where(e => e.IsActive == true && e.IsPublished == true)
                .OrderBy(e => e.EventDate)
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
                                var startDate = d.StartDate;
                                var endDate = d.EndDate;
                                string description = $"Valid until {endDate:MMM dd}";
                                return new PromotionViewModel
                                {
                                    PromotionId = d.DiscountId,
                                    Title = d.DiscountName,
                                    DiscountValue = d.DiscountPercent ?? 0,
                                    DiscountType = "Percentage",
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

            return View(events);
        }

        // GET: /Events/Category/{categoryName}
        public ActionResult Category(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var filteredEvents = db.Events
                .Include(e => e.Venue)
                .Include(e => e.EventDiscounts)
                .Where(e => e.IsActive == true && e.IsPublished == true && e.Category == id)
                .OrderBy(e => e.EventDate)
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
                                var startDate = d.StartDate;
                                var endDate = d.EndDate;
                                string description = $"Valid until {endDate:MMM dd}";
                                return new PromotionViewModel
                                {
                                    PromotionId = d.DiscountId,
                                    Title = d.DiscountName,
                                    DiscountValue = d.DiscountPercent ?? 0,
                                    DiscountType = "Percentage",
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

            ViewBag.CategoryTitle = id;
            return View("Index", filteredEvents);
        }

        // GET: /Events/Details/{id}
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var e = db.Events
                .Include(ev => ev.SeatCategories)
                .Include(ev => ev.EventDiscounts)
                .Include(ev => ev.User)
                .Include(ev => ev.Venue)
                .FirstOrDefault(ev => ev.EventId == id.Value && ev.IsActive == true && ev.IsPublished == true);

            if (e == null)
                return HttpNotFound();

            // Gather active discounts
            var activeDiscounts = e.EventDiscounts
                .Where(d => d.IsActive &&
                            (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                            (d.EndDate == null || d.EndDate >= DateTime.Now))
                .ToList();

            var seatCategoryViewModels = e.SeatCategories.Select(sc =>
            {
                // Find the best discount for this seat category (adjust if discounts are per category)
                var applicableDiscount = activeDiscounts
                    .OrderByDescending(d => d.DiscountPercent ?? 0)
                    .FirstOrDefault();

                decimal? discountedPrice = null;
                string discountLabel = null;

                if (applicableDiscount != null && applicableDiscount.DiscountPercent.HasValue)
                {
                    discountedPrice = sc.Price - (sc.Price * applicableDiscount.DiscountPercent.Value / 100m);
                    discountLabel = $"{applicableDiscount.DiscountName}: {applicableDiscount.DiscountPercent.Value:0.##}% off";
                    // FIX: Remove HasValue/Value (EndDate is not nullable, use directly)
                    if (applicableDiscount.EndDate != null && applicableDiscount.EndDate != DateTime.MinValue)
                        discountLabel += $" (until {applicableDiscount.EndDate:MMM dd})";
                }

                return new SeatCategoryViewModel
                {
                    SeatCategoryId = sc.SeatCategoryId,
                    CategoryName = sc.CategoryName,
                    Price = sc.Price,
                    DiscountedPrice = discountedPrice,
                    TotalSeats = sc.TotalSeats,
                    AvailableSeats = sc.AvailableSeats,
                    DiscountLabel = discountLabel
                };
            }).ToList();

            var viewModel = new EventDetailsViewModel
            {
                EventId = e.EventId,
                Title = e.Title,
                Category = e.Category,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                ImageUrl = e.ImageUrl,
                OrganizerName = e.User != null ? e.User.Username : "Unknown",
                VenueName = e.Venue != null ? e.Venue.VenueName : "",
                VenueAddress = e.Venue != null ? e.Venue.Address : "",
                VenueCapacity = e.Venue != null ? (int?)e.Venue.Capacity : null,
                SeatCategories = seatCategoryViewModels,
                ActiveDiscounts = activeDiscounts.Select(d => new EventDiscountViewModel
                {
                    DiscountId = d.DiscountId,
                    DiscountName = d.DiscountName,
                    DiscountPercent = (decimal)(d.DiscountPercent ?? 0),
                    DiscountAmount = d.DiscountAmount,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    IsActive = d.IsActive
                }).ToList()
            };

            ViewBag.Title = viewModel.Title;
            return View(viewModel);
        }

        // GET: /Events/Search
        public ActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            var events = db.Events
                .Include(ev => ev.SeatCategories)
                .Include(ev => ev.Venue)
                .Include(ev => ev.EventDiscounts)  // Include offers/discounts
                .AsQueryable();

            string loweredQuery = query.Trim().ToLower();

            events = events.Where(ev =>
                ev.Title.ToLower().Contains(loweredQuery) ||
                ev.Category.ToLower().Contains(loweredQuery) ||
                (ev.Venue != null && ev.Venue.VenueName.ToLower().Contains(loweredQuery)) ||
                (ev.EventDate != null && ev.EventDate.ToString().ToLower().Contains(loweredQuery))
            );

            // Bring data into memory BEFORE projecting to EventCardViewModel
            var filteredEvents = events.ToList();

            var results = filteredEvents.Select(ev => new EventCardViewModel
            {
                EventId = ev.EventId,
                Title = ev.Title,
                Category = ev.Category,
                VenueName = ev.Venue != null ? ev.Venue.VenueName : "",
                EventDate = ev.EventDate,
                ImageUrl = ev.ImageUrl,
                StartingPrice = ev.SeatCategories != null && ev.SeatCategories.Any()
        ? ev.SeatCategories.Min(sc => (decimal?)sc.Price) ?? 0
        : 0,
                Promotions = ev.EventDiscounts != null
        ? ev.EventDiscounts
            .Where(d => d.IsActive &&
                        (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                        (d.EndDate == null || d.EndDate >= DateTime.Now))
            .Select(d => {
                var promoType = GetPromotionType(d.DiscountName);
                var (icon, color) = GetIconAndColor(promoType);
                var startDate = d.StartDate;
                var endDate = d.EndDate;
                string description = $"Valid until {endDate:MMM dd}";
                return new PromotionViewModel
                {
                    PromotionId = d.DiscountId,
                    Title = d.DiscountName,
                    DiscountValue = d.DiscountPercent ?? 0,
                    DiscountType = "Percentage",
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
            }).ToList();

            ViewBag.SearchTerm = query;

            return View("SearchResults", results);
        }

        public ActionResult Featured(string filter = "thismonth")
        {
            var now = DateTime.Now;

            var events = db.Events
                .Include(e => e.Venue)
                .Include(e => e.SeatCategories)
                .Include(e => e.EventDiscounts)
                .Where(e => e.IsActive == true && e.IsPublished == true);

            switch (filter)
            {
                case "thismonth":
                    events = events.Where(e => e.EventDate.Month == now.Month && e.EventDate.Year == now.Year);
                    break;
                case "nextmonth":
                    var nextMonth = now.AddMonths(1);
                    events = events.Where(e => e.EventDate.Month == nextMonth.Month && e.EventDate.Year == nextMonth.Year);
                    break;
                case "upcoming":
                    events = events.Where(e => e.EventDate > now);
                    break;;
            }

            // Bring data into memory BEFORE projecting to EventCardViewModel
            var filteredEvents = events.OrderBy(e => e.EventDate).ToList();

            var results = filteredEvents.Select(e => new EventCardViewModel
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
                            var startDate = d.StartDate;
                            var endDate = d.EndDate;
                            string description = $"Valid until {endDate:MMM dd}";
                            return new PromotionViewModel
                            {
                                PromotionId = d.DiscountId,
                                Title = d.DiscountName,
                                DiscountValue = d.DiscountPercent ?? 0,
                                DiscountType = "Percentage",
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
            }).ToList();

            ViewBag.CurrentFilter = filter;

            return View("Index", results); // Or use a separate view if you wish
        }
    }
}