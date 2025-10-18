using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize]
    public class MyBookingsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /MyBookings/
        public ActionResult Index()
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });
            }

            var bookings = db.Bookings
                .Where(b => b.CustomerId == user.UserId)
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            var bookingVMs = new List<BookingDisplayViewModel>();

            foreach (var booking in bookings)
            {
                var payment = db.Payments.FirstOrDefault(p => p.PaymentId == booking.PaymentId);

                bookingVMs.Add(new BookingDisplayViewModel
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    EventTitle = booking.Event.Title,
                    EventDate = booking.Event.EventDate,
                    VenueName = booking.Event.Venue.VenueName,
                    BookingDate = (DateTime)booking.BookingDate,
                    Quantity = booking.Quantity,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    PaymentReference = payment?.PaymentReference
                });
            }

            var model = new MyBookingsViewModel
            {
                Bookings = bookingVMs
            };

            return View(model);
        }

        // Optional: Booking Details Page (legacy)
        public ActionResult Details(int id)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == id);
            if (booking == null)
            {
                return HttpNotFound();
            }

            return View(booking);
        }

        // Booking Details Page
        public ActionResult BookingDetails(int id)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == id && b.CustomerId == user.UserId);
            if (booking == null)
                return HttpNotFound();

            var profile = db.CustomerProfiles.FirstOrDefault(p => p.CustomerId == user.UserId);
            if (profile == null)
                return HttpNotFound("Customer profile not found.");

            var payment = db.Payments.FirstOrDefault(p => p.PaymentId == booking.PaymentId);

            // Map tickets to TicketPartialViewModel for use in _EticketPartial
            var tickets = db.Tickets
                .Where(t => t.BookingId == booking.BookingId)
                .Select(t => new TicketPartialViewModel
                {
                    TicketNumber = t.TicketId, 
                    TicketCode = t.TicketCode,
                    QRCodeUrl = t.QRCodePath,
                    EventTitle = booking.Event.Title,
                    VenueName = booking.Event.Venue.VenueName,
                    EventDate = booking.Event.EventDate,
                    CustomerName = profile.FullName,
                    CustomerPhone = profile.PhoneNumber,
                    CustomerEmail = user.Email,
                    SeatCategory = t.SeatCategory.CategoryName, 
                    BookingCode = booking.BookingCode,
                    PaymentReference = payment.PaymentReference,
                    IsUsed = (bool)t.IsUsed
                }).ToList();

            var model = new BookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                EventTitle = booking.Event.Title,
                EventDate = booking.Event.EventDate,
                VenueName = booking.Event.Venue.VenueName,
                BookingDate = (DateTime)booking.BookingDate,
                Quantity = booking.Quantity,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                PaymentReference = payment.PaymentReference,
                Tickets = tickets 
            };
            return View(model);
        }
    }
}