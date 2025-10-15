using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerEventsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /CustomerEvents/UpcomingEvents
        public ActionResult UpcomingEvents()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null)
                return RedirectToAction("Index", "Home");

            var today = DateTime.Now.Date;

            var bookings = db.Bookings
                .Where(b =>
                    b.CustomerId == user.UserId &&
                    b.Event.EventDate >= today &&
                    b.Event.IsActive == true &&
                    b.Event.IsPublished == true &&
                    (b.Status == "Confirmed" || b.Status == "Paid"))
                .OrderBy(b => b.Event.EventDate)
                .Select(b => new UpcomingEventViewModel
                {
                    BookingId = b.BookingId, // <-- Added!
                    EventId = b.Event.EventId,
                    Title = b.Event.Title,
                    Category = b.Event.Category,
                    EventDate = b.Event.EventDate,
                    Location = b.Event.Location,
                    ImageUrl = b.Event.ImageUrl,
                    BookingCode = b.BookingCode,
                    Quantity = b.Quantity,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status
                })
                .ToList();

            return View(bookings);
        }
    }
}