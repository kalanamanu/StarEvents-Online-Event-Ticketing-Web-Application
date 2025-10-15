using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    public class TicketController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /Ticket/Eticket
        [HttpGet]
        public ActionResult Eticket(int bookingId)
        {
            // Fetch the booking (with Event only, since SeatCategory/CustomerProfile are not navigation properties)
            var booking = db.Bookings
                .Include("Event") // Only if you have an Event navigation property
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return HttpNotFound();
            }

            // Fetch related entities manually since Booking doesn't have nav props for them
            var seatCategory = db.SeatCategories.FirstOrDefault(sc => sc.SeatCategoryId == booking.SeatCategoryId);
            var customerProfile = db.CustomerProfiles.FirstOrDefault(cp => cp.CustomerId == booking.CustomerId);
            var userEntity = db.Users.FirstOrDefault(u => u.UserId == customerProfile.CustomerId);

            var tickets = db.Tickets
                .Where(t => t.BookingId == bookingId)
                .ToList();
            var payment = db.Payments.FirstOrDefault(p => p.PaymentId == booking.PaymentId);

            var model = new ConfirmationViewModel
            {
                EventTitle = booking.Event.Title,
                EventDate = booking.Event.EventDate,
                VenueName = booking.Event.Venue.VenueName,
                SeatCategory = seatCategory?.CategoryName,
                CustomerName = customerProfile?.FullName,
                BookingCode = booking.BookingCode,
                CustomerPhone = customerProfile?.PhoneNumber,
                CustomerEmail = userEntity?.Email,
                PaymentReference = payment?.PaymentReference,
                Tickets = tickets.Select((t, idx) => new ConfirmationTicket
                {
                    TicketNumber = idx + 1,
                    QRCodeUrl = t.QRCodePath,
                    TicketCode = t.TicketCode
                }).ToList()
            };

            return View(model);
        }
    }
}