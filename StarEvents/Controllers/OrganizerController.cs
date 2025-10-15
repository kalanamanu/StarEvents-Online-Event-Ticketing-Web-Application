using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StarEvents.Helpers;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /Organizer/Dashboard
        public ActionResult Dashboard()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            // Get organizer profile
            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Index", "Home");

            // Set session for layout
            Session["OrganizationName"] = organizer.OrganizationName; // Only organization name everywhere
            Session["ProfilePhoto"] = string.IsNullOrEmpty(organizer.ProfilePhoto)
                ? Url.Content("~/Content/images/avatar-default.png")
                : organizer.ProfilePhoto;

            // Gather dashboard stats
            var myEvents = db.Events.Where(e => e.OrganizerId == organizer.OrganizerId).ToList();
            int totalEvents = myEvents.Count;
            int ticketsSold = db.Bookings.Where(b => b.Event.OrganizerId == organizer.OrganizerId && b.Status == "Paid").Sum(b => (int?)b.Quantity) ?? 0;
            decimal revenue = db.Payments
                .Where(p => p.Booking.Event.OrganizerId == organizer.OrganizerId && p.Status == "Paid")
                .Sum(p => (decimal?)p.Amount) ?? 0;

            int unreadNotifications = 0; // Implement if you have a notifications table

            // Upcoming Events
            var upcomingEvents = myEvents
                .Where(e => e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .Select(e => new OrganizerDashboardEventViewModel
                {
                    Id = e.EventId,
                    Title = e.Title,
                    EventDate = e.EventDate
                }).ToList();

            // Recent Bookings (last 5)
            var recentBookings = db.Bookings
                .Where(b => b.Event.OrganizerId == organizer.OrganizerId)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .Select(b => new OrganizerDashboardBookingViewModel
                {
                    CustomerName = b.User.CustomerProfile.FullName ?? b.User.Username,
                    EventTitle = b.Event.Title,
                    Quantity = b.Quantity,
                    BookedAt = (DateTime)b.BookingDate
                }).ToList();

            var viewModel = new OrganizerDashboardViewModel
            {
                TotalEvents = totalEvents,
                TicketsSold = ticketsSold,
                Revenue = revenue,
                UnreadNotifications = unreadNotifications,
                UpcomingEvents = upcomingEvents,
                RecentBookings = recentBookings
            };

            return View(viewName: "Dashboard", model: viewModel);
        }

        // Get profile data
        public ActionResult Profile()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Dashboard");

            // Optionally set session variables if needed
            Session["OrganizationName"] = organizer.OrganizationName;
            Session["ProfilePhoto"] = string.IsNullOrEmpty(organizer.ProfilePhoto)
                ? Url.Content("~/Content/images/avatar-default.png")
                : organizer.ProfilePhoto;

            return View(organizer);
        }

        // GET: /Organizer/EditProfile
        public ActionResult EditProfile()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Dashboard");

            return View(organizer);
        }

        // POST: /Organizer/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(OrganizerProfile model, HttpPostedFileBase ProfilePhotoUpload)
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Dashboard");

            if (ModelState.IsValid)
            {
                organizer.OrganizationName = model.OrganizationName;
                organizer.ContactPerson = model.ContactPerson;
                organizer.PhoneNumber = model.PhoneNumber;
                organizer.Address = model.Address;
                organizer.Description = model.Description;

                // Handle profile photo upload if present
                if (ProfilePhotoUpload != null && ProfilePhotoUpload.ContentLength > 0)
                {
                    var fileName = System.IO.Path.GetFileName(ProfilePhotoUpload.FileName);
                    var filePath = "/uploads/organizer_" + Guid.NewGuid() + "_" + fileName;
                    var serverPath = Server.MapPath("~" + filePath);
                    ProfilePhotoUpload.SaveAs(serverPath);
                    organizer.ProfilePhoto = filePath;
                    Session["ProfilePhoto"] = filePath;
                }

                db.SaveChanges();
                // Update session values
                Session["OrganizationName"] = organizer.OrganizationName;

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            return View(model);
        }

        // GET: /Organizer/CreateEvent
        public ActionResult CreateEvent()
        {
            // No need to fetch venues for ViewBag anymore, since Venue is a custom input
            return View(new CreateEventViewModel
            {
                SeatCategories = new List<SeatCategoryInputViewModel> { new SeatCategoryInputViewModel() }
            });
        }

        // POST: /Organizer/CreateEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEvent(CreateEventViewModel model)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            if (ModelState.IsValid)
            {
                string imageUrl = null;
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    var fileName = System.IO.Path.GetFileName(model.ImageFile.FileName);
                    var filePath = "/uploads/events/" + Guid.NewGuid() + "_" + fileName;
                    var serverPath = Server.MapPath("~" + filePath);
                    model.ImageFile.SaveAs(serverPath);
                    imageUrl = filePath;
                }

                Venue venue = db.Venues.FirstOrDefault(v => v.VenueName == model.VenueName.Trim());
                if (venue == null)
                {
                    venue = new Venue
                    {
                        VenueName = model.VenueName.Trim(),
                        Address = "",
                        City = ""
                    };
                    db.Venues.Add(venue);
                    db.SaveChanges();
                }

                var @event = new Event
                {
                    OrganizerId = organizer.OrganizerId,
                    Title = model.Title,
                    Category = model.Category,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    Location = model.Location,
                    VenueId = venue.VenueId,
                    ImageUrl = imageUrl,
                    IsActive = true,
                    IsPublished = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.Events.Add(@event);
                db.SaveChanges();

                // Log event creation activity
                db.ActivityLogs.Add(new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    ActivityType = "EventCreated",
                    Description = $"Event '{@event.Title}' created by organizer '{organizer.OrganizationName}'.",
                    PerformedBy = organizer.OrganizationName,
                    RelatedEntityId = @event.EventId,
                    EntityType = "Event"
                });
                db.SaveChanges();

                // Add seat categories
                if (model.SeatCategories != null)
                {
                    foreach (var seat in model.SeatCategories)
                    {
                        if (!string.IsNullOrWhiteSpace(seat.CategoryName) && seat.Price > 0 && seat.TotalSeats > 0)
                        {
                            var seatCat = new SeatCategory
                            {
                                EventId = @event.EventId,
                                CategoryName = seat.CategoryName,
                                Price = seat.Price,
                                TotalSeats = seat.TotalSeats,
                                AvailableSeats = seat.TotalSeats
                            };
                            db.SeatCategories.Add(seatCat);
                        }
                    }
                    db.SaveChanges();
                }

                TempData["Success"] = "Event created successfully!";
                return RedirectToAction("MyEvents");
            }

            if (model.SeatCategories == null || model.SeatCategories.Count == 0)
            {
                model.SeatCategories = new List<SeatCategoryInputViewModel> { new SeatCategoryInputViewModel() };
            }
            return View(model);
        }

        // GET: /Organizer/MyEvents
        public ActionResult MyEvents(string search = "")
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            // Get all relevant events (materialize as a list)
            var eventsList = db.Events
                .Where(e => e.OrganizerId == organizer.OrganizerId &&
                    (string.IsNullOrEmpty(search) || e.Title.Contains(search) || e.Category.Contains(search)))
                .OrderByDescending(e => e.EventDate)
                .ToList();

            // Get their IDs
            var eventIds = eventsList.Select(e => e.EventId).ToList();

            // Get all active discounts for these events
            var allPromos = db.EventDiscounts
                .Where(p => eventIds.Contains(p.EventId) && p.IsActive)
                .ToList();

            // Project to your ViewModel in memory (you can now use promoCounts safely)
            var events = eventsList.Select(e => new MyEventListItemViewModel
            {
                EventId = e.EventId,
                Title = e.Title,
                Category = e.Category,
                EventDate = e.EventDate,
                IsPublished = (bool)e.IsPublished,
                Location = e.Location,
                VenueName = e.Venue.VenueName,
                TotalSeats = e.SeatCategories.Sum(sc => (int?)sc.TotalSeats) ?? 0,
                AvailableSeats = e.SeatCategories.Sum(sc => (int?)sc.AvailableSeats) ?? 0,
                // List of discounts for this event
                Discounts = allPromos
                    .Where(p => p.EventId == e.EventId)
                    .Select(p => new DiscountSummary
                    {
                        Name = p.DiscountName,
                        Type = p.DiscountType,
                        Percent = (double?)p.DiscountPercent,
                        Amount = p.DiscountAmount
                    })
                    .ToList()
            }).ToList();

            ViewBag.Search = search;
            return View(events);
        }

        // POST: /Organizer/PublishEvent
        [HttpPost]
        public ActionResult PublishEvent(int id)
        {
            var ev = db.Events.Find(id);
            if (ev != null)
            {
                ev.IsPublished = true;
                db.SaveChanges();
            }
            return RedirectToAction("MyEvents");
        }

        // POST: /Organizer/UnpublishEvent
        [HttpPost]
        public ActionResult UnpublishEvent(int id)
        {
            var ev = db.Events.Find(id);
            if (ev != null)
            {
                ev.IsPublished = false;
                db.SaveChanges();
            }
            return RedirectToAction("MyEvents");
        }

        // POST: /Organizer/DeleteEvent
        [HttpPost]
        public ActionResult DeleteEvent(int id)
        {
            var ev = db.Events.Find(id);
            if (ev != null)
            {
                db.Events.Remove(ev);
                db.SaveChanges();
            }
            return RedirectToAction("MyEvents");
        }

        // GET: /Organizer/EditEvent/{id}
        public ActionResult EditEvent(int id)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            var evt = db.Events.Include("SeatCategories").FirstOrDefault(e => e.EventId == id && e.OrganizerId == organizer.OrganizerId);
            if (evt == null) return HttpNotFound();

            var model = new CreateEventViewModel
            {
                Title = evt.Title,
                Category = evt.Category,
                Description = evt.Description,
                EventDate = evt.EventDate,
                Location = evt.Location,
                VenueName = evt.Venue.VenueName,
                ImageUrl = evt.ImageUrl,
                SeatCategories = evt.SeatCategories.Select(sc => new SeatCategoryInputViewModel
                {
                    CategoryName = sc.CategoryName,
                    Price = sc.Price,
                    TotalSeats = sc.TotalSeats
                }).ToList()
                // Note: Image not pre-filled for security, handle as needed
            };

            ViewBag.EventId = id; // For routing/form posting
            return View(model);
        }

        // POST: /Organizer/EditEvent/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditEvent(int id, CreateEventViewModel model)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            var evt = db.Events.Include("SeatCategories").FirstOrDefault(e => e.EventId == id && e.OrganizerId == organizer.OrganizerId);
            if (evt == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                evt.Title = model.Title;
                evt.Category = model.Category;
                evt.Description = model.Description;
                evt.EventDate = model.EventDate;
                evt.Location = model.Location;
                evt.ImageUrl = model.ImageUrl;
                // Venue update or create
                var venue = db.Venues.FirstOrDefault(v => v.VenueName == model.VenueName.Trim());
                if (venue == null)
                {
                    venue = new Venue
                    {
                        VenueName = model.VenueName.Trim(),
                        Address = "",
                        City = ""
                    };
                    db.Venues.Add(venue);
                    db.SaveChanges();
                }
                evt.VenueId = venue.VenueId;

                // Image upload (optional)
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    var fileName = System.IO.Path.GetFileName(model.ImageFile.FileName);
                    var filePath = "/uploads/events/" + Guid.NewGuid() + "_" + fileName;
                    var serverPath = Server.MapPath("~" + filePath);
                    model.ImageFile.SaveAs(serverPath);
                    evt.ImageUrl = filePath;
                }

                evt.UpdatedAt = DateTime.Now;

                // Update seat categories: Remove all and add new
                db.SeatCategories.RemoveRange(evt.SeatCategories);
                db.SaveChanges();
                foreach (var sc in model.SeatCategories)
                {
                    if (!string.IsNullOrWhiteSpace(sc.CategoryName) && sc.Price > 0 && sc.TotalSeats > 0)
                    {
                        db.SeatCategories.Add(new SeatCategory
                        {
                            EventId = evt.EventId,
                            CategoryName = sc.CategoryName,
                            Price = sc.Price,
                            TotalSeats = sc.TotalSeats,
                            AvailableSeats = sc.TotalSeats // (or adjust if you want to keep old availability)
                        });
                    }
                }

                db.SaveChanges();
                TempData["Success"] = "Event updated successfully!";
                return RedirectToAction("MyEvents");
            }

            ViewBag.EventId = id;
            return View(model);
        }

        //Event Details View
        public ActionResult EventDetails(int id)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            var evt = db.Events
                .Include("SeatCategories")
                .Include("Bookings.User.CustomerProfile")
                .Include("Venue")
                .FirstOrDefault(e => e.EventId == id && e.OrganizerId == organizer.OrganizerId);

            if (evt == null) return HttpNotFound();

            // Get all active discounts for this event
            var discounts = db.EventDiscounts.Where(d => d.EventId == id && d.IsActive).ToList();
            var seatCats = evt.SeatCategories.ToList();

            // This list will have one entry PER (discount x seat category)
            var promotions = new List<PromotionSeatDiscountViewModel>();

            foreach (var d in discounts)
            {
                // If SeatCategory is null or empty, skip
                if (string.IsNullOrWhiteSpace(d.SeatCategory)) continue;

                // Split by comma, and trim
                var categoryList = d.SeatCategory.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                foreach (var cat in categoryList)
                {
                    var seat = seatCats.FirstOrDefault(sc => sc.CategoryName.Equals(cat, StringComparison.OrdinalIgnoreCase));
                    if (seat == null) continue;

                    decimal basePrice = seat.Price;
                    decimal discountedPrice = basePrice;

                    if (d.DiscountType == "percent" && d.DiscountPercent.HasValue)
                        discountedPrice = basePrice - (basePrice * (decimal)d.DiscountPercent.Value / 100M);
                    else if (d.DiscountType == "amount" && d.DiscountAmount.HasValue)
                        discountedPrice = basePrice - d.DiscountAmount.Value;

                    promotions.Add(new PromotionSeatDiscountViewModel
                    {
                        DiscountName = d.DiscountName,
                        SeatCategory = cat,
                        DiscountType = d.DiscountType,
                        DiscountPercent = (double?)d.DiscountPercent,
                        DiscountAmount = d.DiscountAmount,
                        BasePrice = basePrice,
                        DiscountedPrice = discountedPrice
                    });
                }
            }

            var viewModel = new OrganizerEventDetailsViewModel
            {
                EventId = evt.EventId,
                Title = evt.Title,
                Category = evt.Category,
                Description = evt.Description,
                EventDate = evt.EventDate,
                Location = evt.Location,
                VenueName = evt.Venue.VenueName,
                ImageUrl = evt.ImageUrl,
                IsPublished = (bool)evt.IsPublished,
                SeatCategories = evt.SeatCategories.Select(sc => new SeatCategoryInfo
                {
                    CategoryName = sc.CategoryName,
                    Price = sc.Price,
                    TotalSeats = sc.TotalSeats,
                    AvailableSeats = sc.AvailableSeats
                }).ToList(),
                Bookings = evt.Bookings?.Select(b => new BookingInfo
                {
                    CustomerName = b.User.CustomerProfile.FullName ?? b.User.Username,
                    Email = b.User.Email,
                    Quantity = b.Quantity,
                    BookedAt = b.BookingDate ?? DateTime.MinValue,
                    Status = b.Status
                }).OrderByDescending(b => b.BookedAt).ToList() ?? new List<BookingInfo>(),
                TotalSeats = evt.SeatCategories.Sum(sc => (int?)sc.TotalSeats) ?? 0,
                AvailableSeats = evt.SeatCategories.Sum(sc => (int?)sc.AvailableSeats) ?? 0,
                Promotions = promotions
            };

            return View(viewModel);
        }

        public ActionResult EventPromotions(int id)
        {
            var discounts = db.EventDiscounts.Where(d => d.EventId == id).ToList();
            var seatCategories = db.SeatCategories.Where(sc => sc.EventId == id).ToList();

            var promoViewModels = discounts.Select(d =>
            {
                var seat = seatCategories.FirstOrDefault(sc => sc.CategoryName == d.SeatCategory);
                decimal? basePrice = seat?.Price;

                decimal? discountedPrice = null;
                if (basePrice != null)
                {
                    if (d.DiscountPercent.HasValue && d.DiscountPercent.Value > 0)
                        discountedPrice = basePrice.Value - (basePrice.Value * d.DiscountPercent.Value / 100M);
                    else if (d.DiscountAmount.HasValue && d.DiscountAmount.Value > 0)
                        discountedPrice = basePrice.Value - d.DiscountAmount.Value;
                }

                return new PromotionWithSeatPriceViewModel
                {
                    Discount = d,
                    SeatBasePrice = basePrice,
                    SeatDiscountedPrice = discountedPrice
                };
            }).ToList();

            ViewBag.EventId = id;
            return View(promoViewModels);
        }

        // Create Promotion (GET)
        public ActionResult CreateEventPromotion(int eventId)
        {
            var model = new StarEvents.ViewModels.EventDiscountViewModel
            {
                EventId = eventId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                IsActive = true
            };
            var seatCategories = db.SeatCategories
                .Where(sc => sc.EventId == eventId)
                .Select(sc => new StarEvents.ViewModels.SeatCategoryViewModel
                {
                    CategoryName = sc.CategoryName,
                    Price = sc.Price,
                    TotalSeats = sc.TotalSeats,
                    AvailableSeats = sc.AvailableSeats
                })
                .ToList();
            ViewBag.SeatCategories = seatCategories;

            return View(model);
        }

        // Create Promotion (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEventPromotion(EventDiscountViewModel model)
        {
            if (ModelState.IsValid && model.SeatCategory != null)
            {
                foreach (var seatCategory in model.SeatCategory)
                {
                    var discount = new EventDiscount
                    {
                        EventId = model.EventId,
                        DiscountName = model.DiscountName,
                        DiscountPercent = model.DiscountPercent,
                        DiscountAmount = model.DiscountAmount,
                        StartDate = model.StartDate ?? DateTime.Today,
                        EndDate = model.EndDate ?? DateTime.Today.AddDays(7),
                        IsActive = model.IsActive,
                        DiscountType = model.DiscountType,
                        MaxUsage = model.MaxUsage,
                        SeatCategory = seatCategory,
                        Description = model.Description,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    db.EventDiscounts.Add(discount);
                }
                db.SaveChanges();
                return RedirectToAction("EventPromotions", new { id = model.EventId });
            }
            return View(model);
        }

        // GET: /Organizer/EditEventPromotion/{id}
        public ActionResult EditEventPromotion(int id)
        {
            var promotion = db.EventDiscounts.Find(id);
            if (promotion == null)
                return HttpNotFound();

            // Prepare ViewModel
            var model = new EventDiscountViewModel
            {
                DiscountId = promotion.DiscountId,
                EventId = promotion.EventId,
                DiscountName = promotion.DiscountName,
                DiscountType = promotion.DiscountType,
                DiscountPercent = promotion.DiscountPercent,
                DiscountAmount = promotion.DiscountAmount,
                Description = promotion.Description,
                MaxUsage = promotion.MaxUsage,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                // As a comma-separated string for hidden field and JS
                SeatCategory = string.IsNullOrEmpty(promotion.SeatCategory)
                ? new List<string>()
                : promotion.SeatCategory.Split(',').ToList()
            };

            // Load all seat categories for this event
            var seatCategories = db.SeatCategories
                .Where(sc => sc.EventId == promotion.EventId)
                .Select(sc => new SeatCategoryViewModel
                {
                    CategoryName = sc.CategoryName,
                    Price = sc.Price,
                    TotalSeats = sc.TotalSeats,
                    AvailableSeats = sc.AvailableSeats
                }).ToList();

            ViewBag.SeatCategories = seatCategories;

            return View(model);
        }

        // POST: /Organizer/EditEventPromotion/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditEventPromotion(EventDiscountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate seat categories for redisplay
                var seatCategories = db.SeatCategories
                    .Where(sc => sc.EventId == model.EventId)
                    .Select(sc => new SeatCategoryViewModel
                    {
                        CategoryName = sc.CategoryName,
                        Price = sc.Price,
                        TotalSeats = sc.TotalSeats,
                        AvailableSeats = sc.AvailableSeats
                    }).ToList();
                ViewBag.SeatCategories = seatCategories;
                return View(model);
            }

            var promotion = db.EventDiscounts.Find(model.DiscountId);
            if (promotion == null)
                return HttpNotFound();

            // Update all relevant fields
            promotion.DiscountName = model.DiscountName;
            promotion.DiscountType = model.DiscountType; // "percent" or "amount"
            promotion.DiscountPercent = model.DiscountPercent;
            promotion.DiscountAmount = model.DiscountAmount;
            promotion.Description = model.Description;
            promotion.MaxUsage = model.MaxUsage;
            promotion.StartDate = (DateTime)model.StartDate;
            promotion.EndDate = (DateTime)model.EndDate;
            promotion.IsActive = model.IsActive;
            promotion.SeatCategory = model.SeatCategory != null ? string.Join(",", model.SeatCategory) : null;
            promotion.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            // Redirect to promotions list or wherever appropriate
            return RedirectToAction("EventPromotions", new { id = promotion.EventId });
        }


        // Delete Promotion
        [HttpPost]
        public ActionResult DeleteEventPromotion(int id)
        {
            var discount = db.EventDiscounts.Find(id);
            if (discount == null) return HttpNotFound();
            int eventId = discount.EventId;
            db.EventDiscounts.Remove(discount);
            db.SaveChanges();
            return RedirectToAction("EventPromotions", new { id = eventId });
        }

        public ActionResult Promotions()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null) return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null) return RedirectToAction("Dashboard");

            // STEP 1: Get just the Event IDs
            var myEventIds = db.Events
                .Where(e => e.OrganizerId == organizer.OrganizerId)
                .Select(e => e.EventId)
                .ToList();

            // STEP 2: Get all Promotions for those events using IDs (primitive type)
            var allPromos = db.EventDiscounts
                .Where(p => myEventIds.Contains(p.EventId))
                .ToList();

            // If you want the Event objects for display (Title/Date), fetch them
            var myEvents = db.Events
                .Where(e => myEventIds.Contains(e.EventId))
                .ToList();

            // STEP 3: Build your ViewModel list
            var promoList = allPromos.Select(p => new PromotionListViewModel
            {
                Promotion = p,
                Event = myEvents.FirstOrDefault(ev => ev.EventId == p.EventId),
                SeatCategory = db.SeatCategories.FirstOrDefault(sc => sc.EventId == p.EventId && sc.CategoryName == p.SeatCategory),
                UsageCount = 0 // Set this if you have usage logic
            }).ToList();

            ViewBag.EventList = myEvents;
            return View(promoList);
        }

        [HttpPost]
        public ActionResult TogglePromotionStatus(int id)
        {
            var promo = db.EventDiscounts.Find(id);
            if (promo == null) return HttpNotFound();

            promo.IsActive = !promo.IsActive;
            db.SaveChanges();
            return Json(new { success = true, newStatus = promo.IsActive });
        }

        // Event Analytics
        public ActionResult EventAnalytics(int id)
        {
            var eventEntity = db.Events.FirstOrDefault(e => e.EventId == id);
            if (eventEntity == null) return HttpNotFound();

            // All seat categories for this event
            var seatCategories = db.SeatCategories
                .Where(sc => sc.EventId == id)
                .ToList();

            // Tickets and Bookings for this event (all in the DB context)
            var ticketsQuery = db.Tickets.Where(t => t.Booking.EventId == id);

            // Total tickets sold
            int totalTicketsSold = ticketsQuery.Count();

            // Total tickets available: sum of all seat categories for this event
            int totalTickets = seatCategories.Sum(sc => sc.TotalSeats);

            // Fetch all active discounts for this event
            var discounts = db.EventDiscounts
                .Where(d => d.EventId == id && d.IsActive &&
                    (d.EndDate == null || d.EndDate >= DateTime.Now))
                .ToList();

            decimal expectedRevenue = 0;
            decimal potentialDiscount = 0;
            decimal discountedPotentialRevenue = 0;

            // Category breakdown with discount analytics
            var categoryBreakdown = seatCategories.Select(sc =>
            {
                // Find an active discount for this category (string match, handle multiple categories in discount)
                var categoryDiscount = discounts
                    .Where(d => !string.IsNullOrEmpty(d.SeatCategory)
                        && d.SeatCategory.Split(',').Select(x => x.Trim()).Contains(sc.CategoryName))
                    .OrderByDescending(d => d.DiscountPercent ?? 0)
                    .ThenByDescending(d => d.DiscountAmount ?? 0)
                    .FirstOrDefault();

                decimal discountPrice = sc.Price;
                int maxDiscountUsage = 0;
                decimal thisPotentialDiscount = 0;

                if (categoryDiscount != null)
                {
                    maxDiscountUsage = categoryDiscount.MaxUsage ?? sc.TotalSeats;

                    if (categoryDiscount.DiscountType == "percent" && categoryDiscount.DiscountPercent.HasValue)
                    {
                        discountPrice = sc.Price * (1 - (decimal)categoryDiscount.DiscountPercent.Value / 100);
                    }
                    else if (categoryDiscount.DiscountType == "amount" && categoryDiscount.DiscountAmount.HasValue)
                    {
                        discountPrice = sc.Price - categoryDiscount.DiscountAmount.Value;
                    }

                    // The discount applies only to maxDiscountUsage seats, rest are full price
                    thisPotentialDiscount = (sc.Price - discountPrice) * Math.Min(maxDiscountUsage, sc.TotalSeats);
                    discountedPotentialRevenue += discountPrice * Math.Min(maxDiscountUsage, sc.TotalSeats) +
                                                 sc.Price * Math.Max(0, sc.TotalSeats - maxDiscountUsage);
                    potentialDiscount += thisPotentialDiscount;
                }
                else
                {
                    discountedPotentialRevenue += sc.Price * sc.TotalSeats;
                }

                expectedRevenue += sc.Price * sc.TotalSeats;

                return new TicketCategoryAnalytics
                {
                    Category = sc.CategoryName,
                    Sold = sc.Tickets.Count(),
                    TotalSeats = sc.TotalSeats,
                    Price = sc.Price,
                    Revenue = sc.Tickets.Sum(t => t.Booking.TotalAmount),
                    MaxRevenue = sc.Price * sc.TotalSeats,
                    DiscountedPrice = discountPrice,
                    MaxDiscountUsage = maxDiscountUsage,
                    PotentialDiscount = thisPotentialDiscount
                };
            }).ToList();

            // Sales trend (group by booking date - day)
            var salesTrend = ticketsQuery
                .GroupBy(t => DbFunctions.TruncateTime(t.Booking.BookingDate))
                .Select(g => new
                {
                    Date = g.Key,
                    Sold = g.Count(),
                    Revenue = g.Sum(t => (decimal?)t.Booking.TotalAmount) ?? 0
                }).OrderBy(x => x.Date).ToList();

            var model = new EventAnalyticsViewModel
            {
                EventId = eventEntity.EventId,
                EventTitle = eventEntity.Title,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = ticketsQuery.Where(t => t.IsUsed != null).Sum(t => (decimal?)t.Booking.TotalAmount) ?? 0,
                CategoryBreakdown = categoryBreakdown,
                SalesTrend = salesTrend.Select(x => new TicketSalesTrend
                {
                    Date = x.Date ?? DateTime.Now,
                    Sold = x.Sold,
                    Revenue = x.Revenue
                }).ToList(),
                TotalTickets = totalTickets,
                ExpectedRevenue = expectedRevenue,
                Discounts = discounts.Select(d => new EventDiscountSummary
                {
                    DiscountName = d.DiscountName,
                    DiscountType = d.DiscountType,
                    DiscountPercent = d.DiscountPercent.HasValue ? (double?)d.DiscountPercent.Value : null,
                    DiscountAmount = d.DiscountAmount,
                    SeatCategory = d.SeatCategory,
                    MaxUsage = d.MaxUsage,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    Description = d.Description,
                    IsActive = d.IsActive
                }).ToList(),
                DiscountedPotentialRevenue = discountedPotentialRevenue,
                TotalPotentialDiscount = potentialDiscount
            };

            return View(model);
        }

        //Organizer Reports
        public ActionResult Reports(DateTime? fromDate, DateTime? toDate)
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Dashboard");

            var eventsQuery = db.Events.Where(e => e.OrganizerId == organizer.OrganizerId);

            if (fromDate.HasValue)
                eventsQuery = eventsQuery.Where(e => e.EventDate >= fromDate.Value);
            if (toDate.HasValue)
                eventsQuery = eventsQuery.Where(e => e.EventDate <= toDate.Value);

            var events = eventsQuery.ToList();

            // For summary cards
            int totalEvents = events.Count;
            int totalTicketsSold = 0;
            decimal totalRevenue = 0;
            decimal totalDiscountGiven = 0; // Cannot calculate actual discount given with current schema

            var eventSummaries = new List<EventSummary>();
            var salesTrendDict = new SortedDictionary<DateTime, (int tickets, decimal revenue)>();

            foreach (var ev in events)
            {
                var seatCategories = db.SeatCategories.Where(sc => sc.EventId == ev.EventId).ToList();
                var tickets = db.Tickets.Where(t => t.Booking.EventId == ev.EventId).ToList();

                int sold = tickets.Count;
                int seats = seatCategories.Sum(sc => sc.TotalSeats);
                decimal revenue = tickets.Where(t => t.IsUsed != null).Sum(t => (decimal?)t.Booking.TotalAmount) ?? 0;

                totalTicketsSold += sold;
                totalRevenue += revenue;

                // Status: Upcoming/Completed/Ongoing
                string status = ev.EventDate > DateTime.Now
                    ? "Upcoming"
                    : (ev.EventDate < DateTime.Now.AddDays(-1) ? "Completed" : "Ongoing");

                eventSummaries.Add(new EventSummary
                {
                    EventId = ev.EventId,
                    EventTitle = ev.Title,
                    EventDate = ev.EventDate,
                    TicketsSold = sold,
                    TotalSeats = seats,
                    Revenue = revenue,
                    DiscountGiven = 0, // Not tracked per booking/ticket
                    Status = status
                });

                // Sales trend by date (use .Date property directly)
                var salesByDate = tickets
                    .Where(t => t.Booking.BookingDate != null)
                    .GroupBy(t => t.Booking.BookingDate.Value.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(t => t.Booking.TotalAmount) });

                foreach (var s in salesByDate)
                {
                    if (!salesTrendDict.ContainsKey(s.Date))
                        salesTrendDict[s.Date] = (0, 0);
                    salesTrendDict[s.Date] = (
                        salesTrendDict[s.Date].tickets + s.Count,
                        salesTrendDict[s.Date].revenue + s.Revenue
                    );
                }
            }

            // Prepare trend
            var salesTrend = salesTrendDict.Select(kvp => new SalesTrendPoint
            {
                Date = kvp.Key,
                TicketsSold = kvp.Value.tickets,
                Revenue = kvp.Value.revenue
            }).ToList();

            // Discount usage list: Only show available discounts, not usage
            var allEventIds = events.Select(e => e.EventId).ToList();
            var discounts = db.EventDiscounts
                .Where(d => allEventIds.Contains(d.EventId))
                .ToList();

            var discountUsages = discounts.Select(d => new DiscountUsageSummary
            {
                DiscountName = d.DiscountName,
                TimesUsed = 0, // Can't calculate actual usage without tracking
                TotalDiscountGiven = 0 // Can't calculate actual amount without tracking
            }).ToList();

            // Top events by revenue/tickets
            var topEvents = eventSummaries
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .Select(e => new TopEventSummary
                {
                    EventTitle = e.EventTitle,
                    TicketsSold = e.TicketsSold,
                    Revenue = e.Revenue
                }).ToList();

            var model = new OrganizerReportsViewModel
            {
                TotalEvents = totalEvents,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TotalDiscountGiven = totalDiscountGiven,
                Events = eventSummaries,
                SalesTrend = salesTrend,
                DiscountUsages = discountUsages,
                TopEvents = topEvents,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(model);
        }

        //Download reports
        public ActionResult DownloadReports()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
                return RedirectToAction("Index", "Home");

            var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
            if (organizer == null)
                return RedirectToAction("Dashboard");

            var events = db.Events.Where(e => e.OrganizerId == organizer.OrganizerId).ToList();

            // Prepare CSV lines
            var lines = new List<string> {
        "Event,Date,Tickets Sold,Total Seats,Revenue,Status"
    };

            foreach (var ev in events)
            {
                var seatCategories = db.SeatCategories.Where(sc => sc.EventId == ev.EventId).ToList();
                var tickets = db.Tickets.Where(t => t.Booking.EventId == ev.EventId).ToList();
                int sold = tickets.Count;
                int seats = seatCategories.Sum(sc => sc.TotalSeats);
                decimal revenue = tickets.Where(t => t.IsUsed != null).Sum(t => (decimal?)t.Booking.TotalAmount) ?? 0;
                string status = ev.EventDate > DateTime.Now
                    ? "Upcoming"
                    : (ev.EventDate < DateTime.Now.AddDays(-1) ? "Completed" : "Ongoing");

                // Escape CSV if needed
                string eventTitle = $"\"{ev.Title.Replace("\"", "\"\"")}\"";
                lines.Add($"{eventTitle},{ev.EventDate:yyyy-MM-dd},{sold},{seats},\"{revenue:N0}\",{status}");
            }

            var csv = string.Join(Environment.NewLine, lines);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var result = File(bytes, "text/csv", "OrganizerReports.csv");
            return result;
        }

        // GET: /Organizer/Settings
        public ActionResult Settings()
        {
            return View();
        }

        // POST: /Organizer/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid form submission.";
                return RedirectToAction("Settings");
            }

            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Settings");
            }

            if (user.PasswordHash != PasswordHelper.HashPassword(model.CurrentPassword))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction("Settings");
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["Error"] = "New password and confirm password do not match.";
                return RedirectToAction("Settings");
            }
            if (model.NewPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters.";
                return RedirectToAction("Settings");
            }

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Settings");
        }

        // POST: /Organizer/Deactivate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive && u.Role == "Organizer");
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Settings");
            }

            user.IsActive = false;
            db.SaveChanges();

            System.Web.Security.FormsAuthentication.SignOut();

            TempData["Success"] = "Your account has been deactivated.";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Organizer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string confirm, string CurrentPassword)
        {
            if (confirm != "DELETE")
            {
                TempData["Error"] = "You must type DELETE to confirm account deletion.";
                return RedirectToAction("Settings");
            }

            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.Role == "Organizer");
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Settings");
            }

            if (user.PasswordHash != PasswordHelper.HashPassword(CurrentPassword))
            {
                TempData["Error"] = "Current password is incorrect. Account not deleted.";
                return RedirectToAction("Settings");
            }

            db.Users.Remove(user);
            db.SaveChanges();

            System.Web.Security.FormsAuthentication.SignOut();

            TempData["Success"] = "Your account has been deleted permanently.";
            return RedirectToAction("Index", "Home");
        }
    }
}