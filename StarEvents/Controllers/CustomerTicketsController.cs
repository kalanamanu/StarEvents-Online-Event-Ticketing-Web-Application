using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize]
    public class CustomerTicketsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /CustomerTickets/MyTickets
        public ActionResult MyTickets(string search = "", string filter = "all")
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            var profile = db.CustomerProfiles.FirstOrDefault(p => p.CustomerId == user.UserId);

            var ticketsQuery = db.Tickets.Where(t => t.Booking.CustomerId == user.UserId);

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                ticketsQuery = ticketsQuery.Where(t =>
                    t.TicketCode.Contains(search) ||
                    t.Booking.Event.Title.Contains(search));
            }

            // Filter
            if (filter == "used")
                ticketsQuery = ticketsQuery.Where(t => (t.IsUsed ?? false));
            else if (filter == "active")
                ticketsQuery = ticketsQuery.Where(t => !(t.IsUsed ?? false));

            var allTickets = ticketsQuery.ToList();

            var ticketVMs = allTickets.Select(t =>
            {
                // Get Payment using PaymentId (since Booking may not have a Payment navigation property)
                Payment payment = null;
                if (t.Booking.PaymentId != null)
                    payment = db.Payments.FirstOrDefault(p => p.PaymentId == t.Booking.PaymentId);

                return new TicketPartialViewModel
                {
                    TicketNumber = t.TicketId,
                    TicketCode = t.TicketCode,
                    QRCodeUrl = t.QRCodePath,
                    EventTitle = t.Booking.Event.Title,
                    VenueName = t.Booking.Event.Venue.VenueName,
                    EventDate = t.Booking.Event.EventDate,
                    CustomerName = profile != null ? profile.FullName : "",
                    CustomerPhone = profile != null ? profile.PhoneNumber : "",
                    CustomerEmail = user.Email,
                    SeatCategory = t.SeatCategory != null ? t.SeatCategory.CategoryName : "",
                    BookingCode = t.Booking.BookingCode,
                    PaymentReference = payment != null ? payment.PaymentReference : null,
                    IsUsed = t.IsUsed ?? false
                };
            }).ToList();

            var usedCount = db.Tickets.Count(t => t.Booking.CustomerId == user.UserId && (t.IsUsed ?? false));
            var activeCount = db.Tickets.Count(t => t.Booking.CustomerId == user.UserId && !(t.IsUsed ?? false));

            var model = new MyTicketsViewModel
            {
                Tickets = ticketVMs,
                Search = search,
                Filter = filter,
                UsedCount = usedCount,
                ActiveCount = activeCount
            };

            return View(model);
        }
    }
}