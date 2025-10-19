using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using StarEvents.ViewModels;
using StarEvents.Models;
using ClosedXML.Excel;
using Rotativa;
using Rotativa.Options;
using System.Text;
using System.Web;

namespace StarEvents.Controllers
{
    public class AdminController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            int totalUsers = db.Users.Count();
            int totalOrganizers = db.Users.Count(u => u.Role == "Organizer");
            int totalCustomers = db.Users.Count(u => u.Role == "Customer");

            int totalEvents = db.Events.Count();
            int totalTicketsSold = db.Tickets.Count();
            decimal totalRevenue = db.Payments.Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0;

            var salesTrend = db.Tickets
                .Where(t => t.Booking.BookingDate != null && t.Booking.Status == "Paid")
                .GroupBy(t => DbFunctions.TruncateTime(t.Booking.BookingDate))
                .OrderBy(g => g.Key)
                .Select(g => new SalesTrendPoint
                {
                    Date = g.Key.Value,
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(t => (decimal?)t.Booking.TotalAmount) ?? 0
                }).ToList();

            var topEvents = db.Events
                .Select(e => new
                {
                    e.Title,
                    e.EventId
                })
                .ToList()
                .Select(e => new TopEventSummary
                {
                    EventTitle = e.Title,
                    TicketsSold = db.Tickets.Count(t => t.Booking.EventId == e.EventId),
                    Revenue = db.Payments
                        .Where(p => p.Booking.EventId == e.EventId)
                        .Sum(p => (decimal?)p.Amount) ?? 0
                })
                .OrderByDescending(e => e.TicketsSold)
                .Take(5)
                .ToList();

            var cutOffDate = DateTime.Now.AddMonths(-11);
            var userGrowthRaw = db.Users
                .Where(u => u.CreatedAt >= cutOffDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .ToList();

            var userGrowth = userGrowthRaw
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Select(x => new UserGrowthPoint
                {
                    Month = x.Year + "-" + x.Month.ToString("D2"),
                    Count = x.Count
                })
                .ToList();

            var recentActivities = db.ActivityLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new ActivityLogEntry
                {
                    Timestamp = a.Timestamp,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    PerformedBy = a.PerformedBy
                }).ToList();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalOrganizers = totalOrganizers,
                TotalCustomers = totalCustomers,
                TotalEvents = totalEvents,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                SalesTrend = salesTrend,
                RecentActivities = recentActivities,
                TopEvents = topEvents,
                UserGrowth = userGrowth
            };

            return View(model);
        }

        // GET: Admin/Users
        public ActionResult Users(string role = "", string status = "", string search = "")
        {
            var users = db.Users
                .Include(u => u.CustomerProfile)
                .Include(u => u.OrganizerProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                users = users.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                users = users.Where(u => u.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.Username.Contains(search) ||
                    u.Email.Contains(search)
                );
            }

            var userList = users
                .OrderByDescending(u => u.CreatedAt)
                .ToList() // bring all needed data to memory
                .Select(u => new UserListViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    ProfilePhoto =
                        u.Role == "Customer" && u.CustomerProfile != null && !string.IsNullOrEmpty(u.CustomerProfile.ProfilePhoto)
                            ? u.CustomerProfile.ProfilePhoto
                        : u.Role == "Organizer" && u.OrganizerProfile != null && !string.IsNullOrEmpty(u.OrganizerProfile.ProfilePhoto)
                            ? u.OrganizerProfile.ProfilePhoto
                        : null // Or set a default image path if you want
                })
                .ToList();

            ViewBag.FilterRole = role;
            ViewBag.FilterStatus = status;
            ViewBag.FilterSearch = search;

            return View(userList);
        }

        // GET: Admin/EditUser/5
        public ActionResult EditUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            };

            // Case-insensitive role checks
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admin = db.Admins.FirstOrDefault(a => a.AdminId == user.UserId);
                if (admin != null)
                {
                    model.AdminNotes = admin.Notes;
                }
            }
            else if (string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
                if (customer != null)
                {
                    model.CustomerFullName = customer.FullName;
                    model.CustomerPhoneNumber = customer.PhoneNumber;
                    model.CustomerAddress = customer.Address;
                    model.CustomerLoyaltyPoints = customer.LoyaltyPoints;
                    model.CustomerProfilePhoto = customer.ProfilePhoto;
                    model.CustomerDateOfBirth = customer.DateOfBirth;
                    model.CustomerGender = customer.Gender;
                }
            }
            else if (string.Equals(user.Role, "Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
                if (organizer != null)
                {
                    model.OrganizerOrganizationName = organizer.OrganizationName;
                    model.OrganizerContactPerson = organizer.ContactPerson;
                    model.OrganizerPhoneNumber = organizer.PhoneNumber;
                    model.OrganizerAddress = organizer.Address;
                    model.OrganizerDescription = organizer.Description;
                    model.OrganizerProfilePhoto = organizer.ProfilePhoto;
                }
            }

            // Role change is not allowed (enforced in view)
            return View(model);
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(EditUserViewModel model, HttpPostedFileBase ProfilePhotoUpload)
        {
            if (!ModelState.IsValid) return View(model);

            var user = db.Users.Find(model.UserId);
            if (user == null) return HttpNotFound();

            user.Username = model.Username;
            user.Email = model.Email;
            // user.Role = model.Role; // Role change not allowed for safety
            user.IsActive = model.IsActive;

            // Case-insensitive role checks
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admin = db.Admins.FirstOrDefault(a => a.AdminId == user.UserId);
                if (admin != null)
                {
                    admin.Notes = model.AdminNotes;
                }
                // else: optionally, create a new Admin row here
            }
            else if (string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
                if (customer != null)
                {
                    customer.FullName = model.CustomerFullName;
                    customer.PhoneNumber = model.CustomerPhoneNumber;
                    customer.Address = model.CustomerAddress;
                    customer.LoyaltyPoints = model.CustomerLoyaltyPoints ?? 0;
                    customer.DateOfBirth = model.CustomerDateOfBirth;
                    customer.Gender = model.CustomerGender;

                    // Photo upload handling
                    if (ProfilePhotoUpload != null && ProfilePhotoUpload.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid() + System.IO.Path.GetExtension(ProfilePhotoUpload.FileName);
                        var uploadDir = "~/uploads";
                        var uploadPath = System.IO.Path.Combine(Server.MapPath(uploadDir), fileName);
                        ProfilePhotoUpload.SaveAs(uploadPath);
                        customer.ProfilePhoto = "uploads/" + fileName;
                    }
                    else
                    {
                        customer.ProfilePhoto = model.CustomerProfilePhoto; // keep existing if not uploading new
                    }
                }
                // else: optionally, create a CustomerProfiles row here
            }
            else if (string.Equals(user.Role, "Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
                if (organizer != null)
                {
                    organizer.OrganizationName = model.OrganizerOrganizationName;
                    organizer.ContactPerson = model.OrganizerContactPerson;
                    organizer.PhoneNumber = model.OrganizerPhoneNumber;
                    organizer.Address = model.OrganizerAddress;
                    organizer.Description = model.OrganizerDescription;

                    // Photo upload handling
                    if (ProfilePhotoUpload != null && ProfilePhotoUpload.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid() + System.IO.Path.GetExtension(ProfilePhotoUpload.FileName);
                        var uploadDir = "~/uploads";
                        var uploadPath = System.IO.Path.Combine(Server.MapPath(uploadDir), fileName);
                        ProfilePhotoUpload.SaveAs(uploadPath);
                        organizer.ProfilePhoto = "uploads/" + fileName;
                    }
                    else
                    {
                        organizer.ProfilePhoto = model.OrganizerProfilePhoto; // keep existing if not uploading new
                    }
                }
            }

            db.SaveChanges();

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("Users");
        }

        // GET: Admin/UserDetails/5
        public ActionResult UserDetails(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            var model = new UserDetailsViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            // Case-insensitive role checks
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admin = db.Admins.FirstOrDefault(a => a.AdminId == user.UserId);
                if (admin != null)
                {
                    model.AdminNotes = admin.Notes;
                }
            }
            else if (string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
                if (customer != null)
                {
                    model.CustomerFullName = customer.FullName;
                    model.CustomerPhoneNumber = customer.PhoneNumber;
                    model.CustomerAddress = customer.Address;
                    model.CustomerLoyaltyPoints = customer.LoyaltyPoints;
                    model.CustomerProfilePhoto = customer.ProfilePhoto;
                    model.CustomerDateOfBirth = customer.DateOfBirth;
                    model.CustomerGender = customer.Gender;
                }

                // Booking history only for Customer
                var bookings = db.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.CustomerId == user.UserId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToList();

                model.BookingHistory = bookings.Select(b => new UserBookingSummary
                {
                    BookingId = b.BookingId,
                    EventTitle = b.Event.Title,
                    BookingDate = b.BookingDate ?? DateTime.MinValue,
                    Quantity = b.Quantity,
                    Status = b.Status
                }).ToList();
            }
            else if (string.Equals(user.Role, "Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
                if (organizer != null)
                {
                    model.OrganizerOrganizationName = organizer.OrganizationName;
                    model.OrganizerContactPerson = organizer.ContactPerson;
                    model.OrganizerPhoneNumber = organizer.PhoneNumber;
                    model.OrganizerAddress = organizer.Address;
                    model.OrganizerDescription = organizer.Description;
                    model.OrganizerProfilePhoto = organizer.ProfilePhoto;
                }
            }

            return View(model);
        }

        // POST: Admin/ToggleUserActive/5 (add AntiForgeryToken if using in AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUserActive(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            user.IsActive = !user.IsActive;
            db.SaveChanges();

            return Json(new { success = true, newStatus = user.IsActive });
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            // Delete related records based on role
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admin = db.Admins.FirstOrDefault(a => a.AdminId == user.UserId);
                if (admin != null) db.Admins.Remove(admin);
            }
            else if (string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
                if (customer != null) db.CustomerProfiles.Remove(customer);

                var bookings = db.Bookings.Where(b => b.CustomerId == user.UserId).ToList();
                foreach (var booking in bookings)
                {
                    var tickets = db.Tickets.Where(t => t.BookingId == booking.BookingId).ToList();
                    db.Tickets.RemoveRange(tickets);

                    var payments = db.Payments.Where(p => p.BookingId == booking.BookingId).ToList();
                    db.Payments.RemoveRange(payments);

                    db.Bookings.Remove(booking);
                }
            }
            else if (string.Equals(user.Role, "Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var organizer = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
                if (organizer != null) db.OrganizerProfiles.Remove(organizer);

                var events = db.Events.Where(e => e.OrganizerId == user.UserId).ToList();
                foreach (var ev in events)
                {
                    var bookings = db.Bookings.Where(b => b.EventId == ev.EventId).ToList();
                    foreach (var booking in bookings)
                    {
                        var tickets = db.Tickets.Where(t => t.BookingId == booking.BookingId).ToList();
                        db.Tickets.RemoveRange(tickets);

                        var payments = db.Payments.Where(p => p.BookingId == booking.BookingId).ToList();
                        db.Payments.RemoveRange(payments);

                        db.Bookings.Remove(booking);
                    }
                    db.Events.Remove(ev);
                }
            }

            // Remove Activity Logs (if applicable)
            var logs = db.ActivityLogs.Where(a => a.PerformedBy == user.Username).ToList();
            db.ActivityLogs.RemoveRange(logs);

            // Finally, remove the user
            db.Users.Remove(user);

            db.SaveChanges();
            return Json(new { success = true });
        }

        //Event list
        public ActionResult Events(string search = "", string status = "")
        {
            var events = db.Events
                .Include(e => e.User)
                .Include(e => e.Venue)
                .Include(e => e.Bookings.Select(b => b.Tickets))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                events = events.Where(e =>
                    e.Title.Contains(search) ||
                    e.User.Username.Contains(search) ||
                    (e.Venue != null && e.Venue.VenueName.Contains(search))
                );

            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                events = events.Where(e => e.IsActive == isActive);
            }

            var eventList = events.ToList(); // Force query to prevent multiple DB hits

            var model = eventList.Select(e => new AdminEventListViewModel
            {
                EventId = e.EventId,
                Title = e.Title,
                OrganizerName = e.User.Username,
                VenueName = e.Venue != null ? e.Venue.VenueName : "(N/A)",
                EventDate = e.EventDate,
                TicketsSold = e.Bookings.SelectMany(b => b.Tickets).Count(),
                TotalTickets = e.SeatCategories.Sum(sc => sc.TotalSeats),
                Revenue = db.Payments
                    .Where(p => p.Booking.EventId == e.EventId && p.Status == "Paid")
                    .Sum(p => (decimal?)p.Amount) ?? 0,
                Status = (e.IsActive ?? false) ? "Active" : "Inactive"
            })
            .OrderByDescending(e => e.EventDate)
            .ToList();

            ViewBag.FilterSearch = search;
            ViewBag.FilterStatus = status;
            return View(model);
        }

        // GET: Admin/EventDetails/{id}
        public ActionResult EventDetails(int id)
        {
            var e = db.Events
                .Include(ev => ev.User)
                .Include(ev => ev.Venue)
                .Include(ev => ev.SeatCategories)
                .Include(ev => ev.EventDiscounts)
                .Include(ev => ev.Bookings.Select(b => b.Tickets))
                .FirstOrDefault(ev => ev.EventId == id);

            if (e == null) return HttpNotFound();

            var seatCats = e.SeatCategories.ToList();
            var bookings = e.Bookings.ToList();

            var seatSummaries = seatCats.Select(sc => new SeatCategorySummary
            {
                CategoryName = sc.CategoryName,
                Price = sc.Price,
                TotalSeats = sc.TotalSeats,
                AvailableSeats = sc.AvailableSeats
            }).ToList();

            var totalSeats = seatCats.Sum(sc => sc.TotalSeats);
            var availableSeats = seatCats.Sum(sc => sc.AvailableSeats);

            // ---- UPDATED PROMO LOGIC: One row per (discount, seat category) ----
            var promoBreakdown = new List<PromotionSummary>();
            foreach (var d in e.EventDiscounts)
            {
                // Allow multiple seat categories per discount (comma separated string)
                var discountCategories = (d.SeatCategory ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                foreach (var catName in discountCategories)
                {
                    var seatCat = seatCats.FirstOrDefault(sc => sc.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase));
                    if (seatCat == null) continue;

                    decimal basePrice = seatCat.Price;
                    decimal discountedPrice = basePrice;
                    if (d.DiscountType?.ToLower() == "percent" && d.DiscountPercent.HasValue)
                        discountedPrice = basePrice * (1 - d.DiscountPercent.Value / 100m);
                    else if (d.DiscountType?.ToLower() == "amount" && d.DiscountAmount.HasValue)
                        discountedPrice = basePrice - d.DiscountAmount.Value;

                    promoBreakdown.Add(new PromotionSummary
                    {
                        DiscountName = d.DiscountName,
                        SeatCategory = catName,
                        DiscountType = d.DiscountType,
                        DiscountPercent = d.DiscountPercent,
                        DiscountAmount = d.DiscountAmount,
                        BasePrice = basePrice,
                        DiscountedPrice = discountedPrice
                    });
                }
            }

            // Bookings summary 
            var bookingsSummary = bookings
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .Select(b => new BookingSummary
                {
                    CustomerName = b.User.Username,
                    Email = b.User.Email,
                    Quantity = b.Tickets.Count,
                    Status = b.Status,
                    BookedAt = b.BookingDate ?? DateTime.MinValue
                }).ToList();

            var model = new AdminEventDetailsViewModel
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                Category = e.Category,
                ImageUrl = e.ImageUrl,
                EventDate = e.EventDate,
                Location = e.Location,
                VenueName = e.Venue != null ? e.Venue.VenueName : "(N/A)",
                IsPublished = e.IsPublished ?? false,
                OrganizerName = e.User.Username,
                OrganizerEmail = e.User.Email,
                SeatCategories = seatSummaries,
                Promotions = promoBreakdown,
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats,
                Bookings = bookingsSummary
            };

            return View(model);
        }

        //Delete Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEvent(int id)
        {
            var evt = db.Events
                .Include(e => e.SeatCategories)
                .Include(e => e.EventDiscounts)
                .Include(e => e.Bookings.Select(b => b.Tickets))
                .FirstOrDefault(e => e.EventId == id);

            if (evt == null)
                return Json(new { success = false, message = "Event not found." });

            // Delete tickets and bookings
            var bookings = evt.Bookings.ToList();
            foreach (var booking in bookings)
            {
                // Delete all tickets for this booking
                var tickets = booking.Tickets.ToList();
                db.Tickets.RemoveRange(tickets);

                // Optionally: delete related payments if you have a Payments table
                var payments = db.Payments.Where(p => p.BookingId == booking.BookingId).ToList();
                db.Payments.RemoveRange(payments);

                db.Bookings.Remove(booking);
            }

            // Delete seat categories
            db.SeatCategories.RemoveRange(evt.SeatCategories);

            // Delete event discounts
            db.EventDiscounts.RemoveRange(evt.EventDiscounts);

            // Remove the event itself
            db.Events.Remove(evt);

            db.SaveChanges();
            return Json(new { success = true });
        }

        //Active Deactive Event toggle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleEventActive(int id)
        {
            var evt = db.Events.Find(id);

            if (evt == null)
                return Json(new { success = false, message = "Event not found." });

            evt.IsActive = !(evt.IsActive ?? false);

            db.SaveChanges();

            return Json(new { success = true, isActive = evt.IsActive });
        }

        // GET: Admin/Reports
        public ActionResult Reports(string reportType = null)
        {
            // If a specific report type is selected, redirect to that detailed report page
            if (!string.IsNullOrEmpty(reportType))
            {
                // You can adjust logic to render the right view or redirect as preferred
                switch (reportType)
                {
                    case "Sales":
                        return RedirectToAction("SalesReport");
                    case "Event":
                        return RedirectToAction("EventReport");
                    case "User":
                        return RedirectToAction("UserReport");
                    case "Organizer":
                        return RedirectToAction("OrganizerReport");
                }
            }

            // Otherwise, render the main Reports dashboard page with cards
            return View();
        }


        // GET: Admin/SalesReport 
        public ActionResult SalesReport(DateTime? from = null, DateTime? to = null, string eventName = "")
        {
            DateTime fromDate = from ?? DateTime.Today.AddDays(-30);
            DateTime toDate = to ?? DateTime.Today;

            var eventNames = db.Events
                .OrderBy(e => e.Title)
                .Select(e => e.Title)
                .Distinct()
                .ToList();
            ViewBag.EventNames = new SelectList(eventNames);

            string category = "";
            string organizer = "";
            if (!string.IsNullOrEmpty(eventName))
            {
                var ev = db.Events.Include(e => e.User).FirstOrDefault(e => e.Title == eventName);
                if (ev != null)
                {
                    category = ev.Category;
                    organizer = ev.User.Username;
                }
            }
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedOrganizer = organizer;

            var bookings = db.Bookings
                .Include(b => b.Event)
                .Include(b => b.Event.User)
                .Include(b => b.Tickets)
                .Include(b => b.Payments)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);

            if (!string.IsNullOrEmpty(eventName))
                bookings = bookings.Where(b => b.Event.Title.ToLower() == eventName.ToLower());

            var bookingList = bookings.ToList(); // force evaluation so we can SelectMany in memory

            // Main rows (by event/day)
            var grouped = bookingList
                .GroupBy(b => new
                {
                    b.Event.Title,
                    b.Event.Category,
                    Organizer = b.Event.User.Username,
                    Date = b.BookingDate.Value.Date
                })
                .Select(g => new SalesReportRow
                {
                    EventTitle = g.Key.Title,
                    Category = g.Key.Category,
                    Organizer = g.Key.Organizer,
                    Date = g.Key.Date,
                    TicketsSold = g.SelectMany(b => b.Tickets).Count(),
                    Revenue = g.SelectMany(b => b.Payments).Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToList();

            // Analytics: By category
            var byCategory = bookingList
                .GroupBy(b => b.Event.Category)
                .Select(g => new SalesCategorySummary
                {
                    Category = g.Key,
                    TicketsSold = g.SelectMany(b => b.Tickets).Count(),
                    Revenue = g.SelectMany(b => b.Payments).Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Analytics: By event (for top/worst sales)
            var byEvent = bookingList
                .GroupBy(b => b.Event.Title)
                .Select(g => new SalesEventSummary
                {
                    EventTitle = g.Key,
                    TicketsSold = g.SelectMany(b => b.Tickets).Count(),
                    Revenue = g.SelectMany(b => b.Payments).Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0
                })
                .OrderByDescending(x => x.TicketsSold)
                .ToList();

            var topEvents = byEvent.Take(5).ToList();
            var worstEvents = byEvent.OrderBy(x => x.TicketsSold).Take(5).ToList();

            // Analytics: Overall
            int totalTickets = grouped.Sum(r => r.TicketsSold);
            decimal totalRevenue = grouped.Sum(r => r.Revenue);
            decimal avgTicketPrice = totalTickets > 0 ? totalRevenue / totalTickets : 0;

            var model = new SalesReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Category = category,
                EventName = eventName,
                Organizer = organizer,
                Rows = grouped,
                TotalRevenue = totalRevenue,
                TotalTicketsSold = totalTickets,
                AvgTicketPrice = avgTicketPrice,
                ByCategory = byCategory,
                TopEvents = topEvents,
                WorstEvents = worstEvents
            };

            return View(model);
        }

        // AJAX endpoint for autofill
        [HttpGet]
        public ActionResult GetCategoryAndOrganizerForEvent(string eventName)
        {
            var ev = db.Events.Include(e => e.User).FirstOrDefault(e => e.Title == eventName);
            if (ev == null)
                return Json(new { category = "", organizer = "" }, JsonRequestBehavior.AllowGet);

            return Json(new { category = ev.Category, organizer = ev.User.Username }, JsonRequestBehavior.AllowGet);
        }

        //Export salse report
        public ActionResult ExportSalesReport(string exportType, DateTime? from, DateTime? to, string eventName)
        {
            DateTime fromDate = from ?? DateTime.Today.AddDays(-30);
            DateTime toDate = to ?? DateTime.Today;
            var bookings = db.Bookings
                .Include(b => b.Event)
                .Include(b => b.Event.User)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);

            if (!string.IsNullOrEmpty(eventName))
                bookings = bookings.Where(b => b.Event.Title.ToLower() == eventName.ToLower());

            var grouped = bookings
                .GroupBy(b => new
                {
                    b.Event.Title,
                    b.Event.Category,
                    Organizer = b.Event.User.Username,
                    Date = DbFunctions.TruncateTime(b.BookingDate)
                })
                .Select(g => new
                {
                    EventTitle = g.Key.Title,
                    Category = g.Key.Category,
                    Organizer = g.Key.Organizer,
                    Date = g.Key.Date,
                    TicketsSold = g.Sum(b => b.Quantity),
                    Revenue = g.Sum(b => (decimal?)b.TotalAmount) ?? 0
                })
                .ToList();

            if (exportType == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Event,Category,Organizer,Date,Tickets Sold,Revenue");
                foreach (var row in grouped)
                {
                    sb.AppendLine($"\"{row.EventTitle}\",\"{row.Category}\",\"{row.Organizer}\",\"{(row.Date.HasValue ? row.Date.Value.ToString("yyyy-MM-dd") : "-")}\",{row.TicketsSold},\"{row.Revenue}\"");
                }
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                return File(buffer, "text/csv", "SalesReport.csv");
            }
            else if (exportType == "excel")
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("SalesReport");
                    ws.Cell(1, 1).Value = "Event";
                    ws.Cell(1, 2).Value = "Category";
                    ws.Cell(1, 3).Value = "Organizer";
                    ws.Cell(1, 4).Value = "Date";
                    ws.Cell(1, 5).Value = "Tickets Sold";
                    ws.Cell(1, 6).Value = "Revenue";

                    int row = 2;
                    foreach (var item in grouped)
                    {
                        ws.Cell(row, 1).Value = item.EventTitle;
                        ws.Cell(row, 2).Value = item.Category;
                        ws.Cell(row, 3).Value = item.Organizer;
                        ws.Cell(row, 4).Value = item.Date?.ToString("yyyy-MM-dd") ?? "-";
                        ws.Cell(row, 5).Value = item.TicketsSold;
                        ws.Cell(row, 6).Value = item.Revenue;
                        row++;
                    }
                    using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(400, "Export type not implemented");
            }
        }


        // GET: Admin/EventReport 
        public ActionResult EventReport(string search = "", string status = "", string organizer = "", DateTime? from = null, DateTime? to = null)
        {
            var events = db.Events
                .Include(e => e.User)
                .Include(e => e.Venue)
                .Include(e => e.SeatCategories)
                .Include(e => e.Bookings.Select(b => b.Tickets))
                .AsQueryable();

            // Filter by search (event title, venue, organizer)
            if (!string.IsNullOrEmpty(search))
                events = events.Where(e =>
                    e.Title.Contains(search) ||
                    (e.Venue != null && e.Venue.VenueName.Contains(search)) ||
                    e.User.Username.Contains(search)
                );

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                events = events.Where(e => (e.IsActive ?? false) == isActive);
            }

            // Filter by organizer
            if (!string.IsNullOrEmpty(organizer))
                events = events.Where(e => e.User.Username == organizer);

            // Filter by event date
            if (from.HasValue)
                events = events.Where(e => e.EventDate >= from.Value);
            if (to.HasValue)
                events = events.Where(e => e.EventDate <= to.Value);

            var eventList = events.ToList();

            // For organizer effectiveness metrics
            var organizerMetrics = eventList
                .GroupBy(e => e.User.Username)
                .Select(g => new
                {
                    Organizer = g.Key,
                    EventCount = g.Count(),
                    TotalTickets = g.Sum(ev => ev.SeatCategories.Sum(sc => sc.TotalSeats)),
                    TotalTicketsSold = g.Sum(ev => ev.Bookings.SelectMany(b => b.Tickets).Count()),
                    TotalRevenue = g.Sum(ev => db.Payments.Where(p => p.Booking.EventId == ev.EventId && p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0)
                })
                .ToList();

            var rows = eventList.Select(e => {
                int eventTotalTickets = e.SeatCategories.Sum(sc => sc.TotalSeats);
                int ticketsSold = e.Bookings.SelectMany(b => b.Tickets).Count();
                decimal revenue = db.Payments.Where(p => p.Booking.EventId == e.EventId && p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0;
                double selloutPct = (eventTotalTickets > 0) ? (double)ticketsSold / eventTotalTickets * 100 : 0;
                decimal avgTicketPrice = ticketsSold > 0 ? revenue / ticketsSold : 0;
                return new EventReportRow
                {
                    EventTitle = e.Title,
                    Category = e.Category,
                    Organizer = e.User.Username,
                    EventDate = e.EventDate,
                    Venue = e.Venue != null ? e.Venue.VenueName : "(N/A)",
                    TotalTickets = eventTotalTickets,
                    TicketsSold = ticketsSold,
                    Revenue = revenue,
                    SelloutRatePct = selloutPct,
                    AvgTicketPrice = avgTicketPrice,
                    Status = (e.IsActive ?? false) ? "Active" : "Inactive"
                };
            }).ToList();

            var allOrganizers = db.Users.Where(u => u.Role == "Organizer").Select(u => u.Username).Distinct().ToList();
            var statusList = new List<string> { "Active", "Inactive" };

            // Platform totals
            int totalEvents = rows.Count;
            int totalTickets = rows.Sum(r => r.TotalTickets);
            int totalTicketsSold = rows.Sum(r => r.TicketsSold);
            decimal totalRevenue = rows.Sum(r => r.Revenue);
            double avgSelloutPct = rows.Count > 0 ? rows.Average(r => r.SelloutRatePct) : 0;
            decimal avgTicketPriceAll = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0;

            // Organizer effectiveness summaries (for view)
            var organizerSummaries = organizerMetrics.Select(o => new OrganizerEffectivenessSummary
            {
                Organizer = o.Organizer,
                EventCount = o.EventCount,
                TotalTickets = o.TotalTickets,
                TotalTicketsSold = o.TotalTicketsSold,
                TotalRevenue = o.TotalRevenue,
                AvgTicketsPerEvent = o.EventCount > 0 ? (double)o.TotalTicketsSold / o.EventCount : 0,
                AvgRevenuePerEvent = o.EventCount > 0 ? o.TotalRevenue / o.EventCount : 0
            }).ToList();

            var vm = new EventReportViewModel
            {
                Rows = rows,
                TotalEvents = totalEvents,
                TotalTickets = totalTickets,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                AvgSelloutRatePct = avgSelloutPct,
                AvgTicketPrice = avgTicketPriceAll,
                Search = search,
                Status = status,
                Organizer = organizer,
                From = from,
                To = to,
                OrganizerList = allOrganizers,
                StatusList = statusList,
                OrganizerSummaries = organizerSummaries
            };

            return View(vm);
        }

        // EXPORT: CSV and Excel for Event Report with filters
        public ActionResult ExportEventReport(string exportType, string search = "", string status = "", string organizer = "", DateTime? from = null, DateTime? to = null)
        {
            var events = db.Events
                .Include(e => e.User)
                .Include(e => e.Venue)
                .Include(e => e.SeatCategories)
                .Include(e => e.Bookings.Select(b => b.Tickets))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                events = events.Where(e =>
                    e.Title.Contains(search) ||
                    (e.Venue != null && e.Venue.VenueName.Contains(search)) ||
                    e.User.Username.Contains(search)
                );
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                events = events.Where(e => (e.IsActive ?? false) == isActive);
            }
            if (!string.IsNullOrEmpty(organizer))
                events = events.Where(e => e.User.Username == organizer);
            if (from.HasValue)
                events = events.Where(e => e.EventDate >= from.Value);
            if (to.HasValue)
                events = events.Where(e => e.EventDate <= to.Value);

            var eventList = events.ToList();

            var rows = eventList.Select(e => new EventReportRow
            {
                EventTitle = e.Title,
                Category = e.Category,
                Organizer = e.User.Username,
                EventDate = e.EventDate,
                Venue = e.Venue != null ? e.Venue.VenueName : "(N/A)",
                TotalTickets = e.SeatCategories.Sum(sc => sc.TotalSeats),
                TicketsSold = e.Bookings.Where(b => b.Status == "Confirmed").SelectMany(b => b.Tickets).Count(),
                Revenue = db.Payments.Where(p => p.Booking.EventId == e.EventId && p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0,
                Status = (e.IsActive ?? false) ? "Active" : "Inactive"
            }).ToList();

            if (exportType == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Event,Category,Organizer,Event Date,Venue,Total Tickets,Tickets Sold,Revenue,Status");
                foreach (var row in rows)
                {
                    sb.AppendLine($"\"{row.EventTitle}\",\"{row.Category}\",\"{row.Organizer}\",\"{(row.EventDate.HasValue ? row.EventDate.Value.ToString("yyyy-MM-dd") : "-")}\",\"{row.Venue}\",{row.TotalTickets},{row.TicketsSold},\"{row.Revenue}\",\"{row.Status}\"");
                }
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                return File(buffer, "text/csv", "EventReport.csv");
            }
            else if (exportType == "excel")
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("EventReport");
                    ws.Cell(1, 1).Value = "Event";
                    ws.Cell(1, 2).Value = "Category";
                    ws.Cell(1, 3).Value = "Organizer";
                    ws.Cell(1, 4).Value = "Event Date";
                    ws.Cell(1, 5).Value = "Venue";
                    ws.Cell(1, 6).Value = "Total Tickets";
                    ws.Cell(1, 7).Value = "Tickets Sold";
                    ws.Cell(1, 8).Value = "Revenue";
                    ws.Cell(1, 9).Value = "Status";

                    int rowNum = 2;
                    foreach (var row in rows)
                    {
                        ws.Cell(rowNum, 1).Value = row.EventTitle;
                        ws.Cell(rowNum, 2).Value = row.Category;
                        ws.Cell(rowNum, 3).Value = row.Organizer;
                        ws.Cell(rowNum, 4).Value = row.EventDate?.ToString("yyyy-MM-dd") ?? "-";
                        ws.Cell(rowNum, 5).Value = row.Venue;
                        ws.Cell(rowNum, 6).Value = row.TotalTickets;
                        ws.Cell(rowNum, 7).Value = row.TicketsSold;
                        ws.Cell(rowNum, 8).Value = row.Revenue;
                        ws.Cell(rowNum, 9).Value = row.Status;
                        rowNum++;
                    }
                    using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EventReport.xlsx");
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(400, "Export type not implemented");
            }
        }

        // GET: Admin/UserReport
        public ActionResult UserReport(string search = "", string role = "", string status = "", DateTime? from = null, DateTime? to = null)
        {
            var users = db.Users.AsQueryable();

            // Filter by search (username/email)
            if (!string.IsNullOrEmpty(search))
                users = users.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

            // Filter by role
            if (!string.IsNullOrEmpty(role))
                users = users.Where(u => u.Role == role);

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                users = users.Where(u => u.IsActive == isActive);
            }

            // Filter by date range
            if (from.HasValue)
                users = users.Where(u => u.CreatedAt >= from.Value);
            if (to.HasValue)
                users = users.Where(u => u.CreatedAt <= to.Value);

            var userList = users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserReportRow
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            var roles = db.Users.Select(u => u.Role).Distinct().ToList();
            var statusList = new List<string> { "Active", "Inactive" };

            // --- Analytics Additions ---

            var now = DateTime.Today;
            var regTrendCutoff = now.AddMonths(-11);

            // Get raw data from database, no ToString or string concatenation here!
            var regTrendsRaw = db.Users
                .Where(u => u.CreatedAt >= regTrendCutoff)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // Do string formatting in-memory
            var regTrends = regTrendsRaw
                .Select(x => new RegistrationTrendPoint
                {
                    Period = x.Year + "-" + x.Month.ToString("D2"),
                    Count = x.Count
                })
                .ToList();

            var customers = db.Users.Where(u => u.Role == "Customer").ToList();
            var customerProfiles = db.CustomerProfiles.ToList();

            var totalLoyalty = customerProfiles.Sum(cp => (int?)cp.LoyaltyPoints) ?? 0;
            var avgLoyalty = customerProfiles.Any() ? customerProfiles.Average(cp => (int)cp.LoyaltyPoints) : 0;
            var topLoyalty = customerProfiles
                .OrderByDescending(cp => cp.LoyaltyPoints)
                .Take(5)
                .Join(customers, cp => cp.CustomerId, u => u.UserId, (cp, u) => new LoyaltyUserSummary
                {
                    Username = u.Username,
                    Points = (int)cp.LoyaltyPoints
                }).ToList();

            var segments = new List<CustomerSegmentSummary>
    {
        new CustomerSegmentSummary
        {
            SegmentName = "Loyalty Points >= 1000",
            Description = "Users with 1000 or more loyalty points",
            UserCount = customerProfiles.Count(cp => cp.LoyaltyPoints >= 1000)
        },
        new CustomerSegmentSummary
        {
            SegmentName = "Loyalty Points 100-999",
            Description = "Users with 100 to 999 loyalty points",
            UserCount = customerProfiles.Count(cp => cp.LoyaltyPoints >= 100 && cp.LoyaltyPoints < 1000)
        },
        new CustomerSegmentSummary
        {
            SegmentName = "Loyalty Points < 100",
            Description = "Users with less than 100 loyalty points",
            UserCount = customerProfiles.Count(cp => cp.LoyaltyPoints < 100)
        }
    };

            var topActivities = db.ActivityLogs
                .GroupBy(a => a.ActivityType)
                .Select(g => new UserBehaviorSummary
                {
                    Activity = g.Key,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count).Take(5).ToList();

            var bookingCounts = customers.Select(u => db.Bookings.Count(b => b.CustomerId == u.UserId)).ToList();
            double avgBookings = bookingCounts.Any() ? bookingCounts.Average() : 0;

            var thirtyDaysAgo = now.AddDays(-30);
            int retained = customers.Count(u => db.Bookings.Any(b => b.CustomerId == u.UserId && b.BookingDate >= thirtyDaysAgo));
            double retention30 = customers.Count > 0 ? retained / (double)customers.Count * 100 : 0;

            var vm = new UserReportAnalyticsViewModel
            {
                // Existing
                Rows = userList,
                TotalUsers = userList.Count,
                TotalActive = userList.Count(u => u.IsActive),
                TotalCustomers = userList.Count(u => u.Role == "Customer"),
                TotalOrganizers = userList.Count(u => u.Role == "Organizer"),
                // Filters
                Search = search,
                Role = role,
                Status = status,
                From = from,
                To = to,
                RoleList = roles,
                StatusList = statusList,
                // Analytics
                RegistrationTrends = regTrends,
                TotalLoyaltyPoints = totalLoyalty,
                AvgLoyaltyPoints = avgLoyalty,
                TopLoyaltyCustomers = topLoyalty,
                CustomerSegments = segments,
                TopUserActivities = topActivities,
                AvgBookingsPerUser = avgBookings,
                RetentionRates = new Dictionary<string, double> { { "30-day", retention30 } }
            };

            return View(vm);
        }

        // Export User Report (no change, unless you want analytics in export)
        public ActionResult ExportUserReport(string exportType, string search = "", string role = "", string status = "", DateTime? from = null, DateTime? to = null)
        {
            var users = db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                users = users.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            if (!string.IsNullOrEmpty(role))
                users = users.Where(u => u.Role == role);
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "Active";
                users = users.Where(u => u.IsActive == isActive);
            }
            if (from.HasValue)
                users = users.Where(u => u.CreatedAt >= from.Value);
            if (to.HasValue)
                users = users.Where(u => u.CreatedAt <= to.Value);

            var userList = users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserReportRow
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                }).ToList();

            if (exportType == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Username,Email,Role,Status,Created At");
                foreach (var row in userList)
                {
                    sb.AppendLine($"\"{row.Username}\",\"{row.Email}\",\"{row.Role}\",\"{(row.IsActive ? "Active" : "Inactive")}\",\"{row.CreatedAt:yyyy-MM-dd}\"");
                }
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                return File(buffer, "text/csv", "UserReport.csv");
            }
            else if (exportType == "excel")
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("UserReport");
                    ws.Cell(1, 1).Value = "Username";
                    ws.Cell(1, 2).Value = "Email";
                    ws.Cell(1, 3).Value = "Role";
                    ws.Cell(1, 4).Value = "Status";
                    ws.Cell(1, 5).Value = "Created At";

                    int rowNum = 2;
                    foreach (var row in userList)
                    {
                        ws.Cell(rowNum, 1).Value = row.Username;
                        ws.Cell(rowNum, 2).Value = row.Email;
                        ws.Cell(rowNum, 3).Value = row.Role;
                        ws.Cell(rowNum, 4).Value = row.IsActive ? "Active" : "Inactive";
                        ws.Cell(rowNum, 5).Value = row.CreatedAt.ToString("yyyy-MM-dd");
                        rowNum++;
                    }
                    using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserReport.xlsx");
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(400, "Export type not implemented");
            }
        }

        // GET: Admin/OrganizerReport
        public ActionResult OrganizerReport(DateTime? from = null, DateTime? to = null, string search = "")
        {
            var organizers = db.Users.Where(u => u.Role == "Organizer");

            if (!string.IsNullOrEmpty(search))
                organizers = organizers.Where(o => o.Username.Contains(search) || o.Email.Contains(search));

            // Optionally, filter events by date range
            var events = db.Events.AsQueryable();
            if (from.HasValue)
                events = events.Where(e => e.EventDate >= from.Value);
            if (to.HasValue)
                events = events.Where(e => e.EventDate <= to.Value);

            var eventList = events.ToList();

            // Gather all tickets and payments once to avoid repeated queries
            var allTickets = db.Tickets.Include(t => t.Booking.Event).ToList();
            var allPayments = db.Payments.ToList();

            // For overall revenue (for contribution %)
            decimal totalRevenueAll = allPayments.Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0;

            var rows = organizers
                .ToList()
                .Select(o =>
                {
                    // Filter organizer's events
                    var orgEvents = eventList.Where(e => e.OrganizerId == o.UserId).ToList();
                    var orgEventIds = orgEvents.Select(e => e.EventId).ToList();

                    // Productivity metrics
                    int eventCount = orgEvents.Count;
                    int ticketsSold = allTickets.Count(t => t.Booking != null && t.Booking.Event != null && orgEventIds.Contains(t.Booking.Event.EventId));
                    double avgEventsPerMonth = 0;
                    if (eventCount > 0)
                    {
                        var firstEventDate = orgEvents.Min(e => e.EventDate);
                        int months = ((DateTime.Now.Year - firstEventDate.Year) * 12) + DateTime.Now.Month - firstEventDate.Month + 1;
                        avgEventsPerMonth = months > 0 ? (double)eventCount / months : eventCount;
                    }

                    // Revenue
                    decimal revenue = allPayments.Where(p => orgEventIds.Contains(p.Booking.EventId) && p.Status == "Paid")
                                                 .Sum(p => (decimal?)p.Amount) ?? 0;
                    double revenuePct = totalRevenueAll > 0 ? ((double)revenue / (double)totalRevenueAll) * 100 : 0;

                    // Event quality metrics
                    double avgTicketsPerEvent = eventCount > 0 ? (double)ticketsSold / eventCount : 0;

                    // Sellout rate (if you have MaxTickets in Event)
                    double selloutRate = 0;
                    var maxTicketsProp = typeof(Event).GetProperty("MaxTickets");
                    if (eventCount > 0 && maxTicketsProp != null)
                    {
                        int selloutEvents = orgEvents.Count(ev =>
                        {
                            var maxTickets = maxTicketsProp.GetValue(ev);
                            int max = maxTickets is int ? (int)maxTickets : 0;
                            int sold = allTickets.Count(t => t.Booking != null && t.Booking.Event != null && t.Booking.Event.EventId == ev.EventId);
                            return max > 0 && sold >= max;
                        });
                        selloutRate = 100.0 * selloutEvents / eventCount;
                    }

                    // Partnership events (if IsPartnership exists)
                    int partnershipEvents = 0;
                    var isPartnershipProp = typeof(Event).GetProperty("IsPartnership");
                    if (isPartnershipProp != null)
                    {
                        partnershipEvents = orgEvents.Count(e =>
                        {
                            var val = isPartnershipProp.GetValue(e);
                            return val is bool b && b;
                        });
                    }

                    return new OrganizerReportRow
                    {
                        OrganizerId = o.UserId,
                        OrganizerName = o.Username,
                        Email = o.Email,
                        EventCount = eventCount,
                        TotalTicketsSold = ticketsSold,
                        TotalRevenue = revenue,
                        RevenueContributionPct = revenuePct,
                        AvgEventsPerMonth = avgEventsPerMonth,
                        AvgTicketsPerEvent = avgTicketsPerEvent,
                        SelloutRatePct = selloutRate,
                        PartnershipEvents = partnershipEvents
                    };
                }).ToList();

            var vm = new OrganizerReportViewModel
            {
                Rows = rows,
                TotalOrganizers = rows.Count,
                TotalRevenue = rows.Sum(r => r.TotalRevenue),
                TotalEvents = rows.Sum(r => r.EventCount),
                From = from,
                To = to,
                Search = search
            };
            return View(vm);
        }

        // EXPORT: Organizer Report (CSV & Excel)
        public ActionResult ExportOrganizerReport(string exportType, DateTime? from = null, DateTime? to = null, string search = "")
        {
            var organizers = db.Users.Where(u => u.Role == "Organizer");

            if (!string.IsNullOrEmpty(search))
                organizers = organizers.Where(o => o.Username.Contains(search) || o.Email.Contains(search));

            var events = db.Events.AsQueryable();
            if (from.HasValue)
                events = events.Where(e => e.EventDate >= from.Value);
            if (to.HasValue)
                events = events.Where(e => e.EventDate <= to.Value);

            var eventList = events.ToList();
            var allTickets = db.Tickets.Include(t => t.Booking.Event).ToList();
            var allPayments = db.Payments.ToList();

            var rows = organizers
                .ToList()
                .Select(o =>
                {
                    var orgEvents = eventList.Where(e => e.OrganizerId == o.UserId).ToList();
                    var orgEventIds = orgEvents.Select(e => e.EventId).ToList();

                    var ticketsSold = allTickets.Count(t => t.Booking != null && t.Booking.Event != null && orgEventIds.Contains(t.Booking.Event.EventId));
                    var revenue = allPayments.Where(p => orgEventIds.Contains(p.Booking.EventId) && p.Status == "Paid")
                                             .Sum(p => (decimal?)p.Amount) ?? 0;

                    int partnershipEvents = 0;
                    var isPartnershipProp = typeof(Event).GetProperty("IsPartnership");
                    if (isPartnershipProp != null)
                    {
                        partnershipEvents = orgEvents.Count(e =>
                        {
                            var val = isPartnershipProp.GetValue(e);
                            return val is bool b && b;
                        });
                    }

                    return new OrganizerReportRow
                    {
                        OrganizerId = o.UserId,
                        OrganizerName = o.Username,
                        Email = o.Email,
                        EventCount = orgEvents.Count,
                        TotalTicketsSold = ticketsSold,
                        TotalRevenue = revenue,
                        PartnershipEvents = partnershipEvents
                    };
                }).ToList();

            if (exportType == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Organizer,Email,Event Count,Tickets Sold,Revenue,Partnership Events");
                foreach (var row in rows)
                {
                    sb.AppendLine($"\"{row.OrganizerName}\",\"{row.Email}\",{row.EventCount},{row.TotalTicketsSold},\"{row.TotalRevenue}\",{row.PartnershipEvents}");
                }
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                return File(buffer, "text/csv", "OrganizerReport.csv");
            }
            else if (exportType == "excel")
            {
                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("OrganizerReport");
                    ws.Cell(1, 1).Value = "Organizer";
                    ws.Cell(1, 2).Value = "Email";
                    ws.Cell(1, 3).Value = "Event Count";
                    ws.Cell(1, 4).Value = "Tickets Sold";
                    ws.Cell(1, 5).Value = "Revenue";
                    ws.Cell(1, 6).Value = "Partnership Events";

                    int rowNum = 2;
                    foreach (var row in rows)
                    {
                        ws.Cell(rowNum, 1).Value = row.OrganizerName;
                        ws.Cell(rowNum, 2).Value = row.Email;
                        ws.Cell(rowNum, 3).Value = row.EventCount;
                        ws.Cell(rowNum, 4).Value = row.TotalTicketsSold;
                        ws.Cell(rowNum, 5).Value = row.TotalRevenue;
                        ws.Cell(rowNum, 6).Value = row.PartnershipEvents;
                        rowNum++;
                    }
                    using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrganizerReport.xlsx");
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(400, "Export type not implemented");
            }
        }

        // GET: Admin/ActivityLog
        public ActionResult ActivityLog(string search = "", string activityType = "", DateTime? from = null, DateTime? to = null)
        {
            var logs = db.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(a =>
                    a.User.Username.Contains(search) ||
                    a.ActivityType.Contains(search) ||
                    a.Description.Contains(search)
                );
            }

            if (!string.IsNullOrEmpty(activityType))
                logs = logs.Where(a => a.ActivityType == activityType);

            if (from.HasValue)
                logs = logs.Where(a => a.Timestamp >= from.Value);

            if (to.HasValue)
                logs = logs.Where(a => a.Timestamp <= to.Value);

            var activityTypes = db.ActivityLogs.Select(a => a.ActivityType).Distinct().ToList();

            var rows = logs
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new ActivityLogRow
                {
                    Username = a.User.Username,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    Timestamp = a.Timestamp
                })
                .ToList();

            var vm = new ActivityLogViewModel
            {
                Rows = rows,
                ActivityTypeList = activityTypes,
                Search = search,
                SelectedActivityType = activityType,
                From = from,
                To = to,
                TotalLogs = rows.Count
            };

            return View(vm);
        }

        // Export Logs
        public ActionResult ExportActivityLog(string exportType, string search = "", string activityType = "", DateTime? from = null, DateTime? to = null)
        {
            var logs = db.ActivityLogs.Include(a => a.User).AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(a =>
                    a.User.Username.Contains(search) ||
                    a.ActivityType.Contains(search) ||
                    a.Description.Contains(search)
                );
            }

            if (!string.IsNullOrEmpty(activityType))
                logs = logs.Where(a => a.ActivityType == activityType);

            if (from.HasValue)
                logs = logs.Where(a => a.Timestamp >= from.Value);

            if (to.HasValue)
                logs = logs.Where(a => a.Timestamp <= to.Value);

            var rows = logs
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new ActivityLogRow
                {
                    Username = a.User.Username,
                    ActivityType = a.ActivityType,
                    Description = a.Description,
                    Timestamp = a.Timestamp
                })
                .ToList();

            if (exportType == "csv")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Username,Activity Type,Description,Timestamp");
                foreach (var row in rows)
                {
                    sb.AppendLine($"\"{row.Username}\",\"{row.ActivityType}\",\"{row.Description}\",\"{row.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
                }
                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                return File(buffer, "text/csv", "ActivityLog.csv");
            }
            else if (exportType == "excel")
            {
                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("ActivityLog");
                    ws.Cell(1, 1).Value = "Username";
                    ws.Cell(1, 2).Value = "Activity Type";
                    ws.Cell(1, 3).Value = "Description";
                    ws.Cell(1, 4).Value = "Timestamp";

                    int rowNum = 2;
                    foreach (var row in rows)
                    {
                        ws.Cell(rowNum, 1).Value = row.Username;
                        ws.Cell(rowNum, 2).Value = row.ActivityType;
                        ws.Cell(rowNum, 3).Value = row.Description;
                        ws.Cell(rowNum, 4).Value = row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                        rowNum++;
                    }
                    using (var stream = new System.IO.MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ActivityLog.xlsx");
                    }
                }
            }
            else
            {
                return new HttpStatusCodeResult(400, "Export type not implemented");
            }
        }
    }
}