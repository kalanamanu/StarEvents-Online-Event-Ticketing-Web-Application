using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerBookingsController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // 1. List of events by this organizer
        public ActionResult Bookings()
        {
            var organizer = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (organizer == null) return HttpNotFound();

            var organizerProfile = organizer.OrganizerProfile;
            if (organizerProfile == null) return HttpNotFound();

            var events = db.Events
                .Where(e => e.OrganizerId == organizerProfile.OrganizerId)
                .Select(e => new OrganizerEventViewModel
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    EventDate = e.EventDate,
                    VenueName = e.Venue.VenueName,
                    BookingCount = e.Bookings.Count()
                }).ToList();

            return View(events);
        }

        // 2. List of bookings for a selected event
        public ActionResult EventBookings(int eventId, string search = "")
        {
            var eventEntity = db.Events.FirstOrDefault(e => e.EventId == eventId);
            if (eventEntity == null) return HttpNotFound();

            var bookingsQuery = eventEntity.Bookings.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.BookingId.ToString().Contains(search) ||
                    b.User.CustomerProfile.FullName.Contains(search));
            }

            var model = new OrganizerBookingListViewModel
            {
                EventId = eventEntity.EventId,
                EventTitle = eventEntity.Title,
                Bookings = bookingsQuery.Select(b => new OrganizerBookingItemViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode, 
                    CustomerName = b.User.CustomerProfile.FullName,
                    CustomerEmail = b.User.Email,
                    BookingDate = (DateTime)b.BookingDate,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount
                }).OrderByDescending(b => b.BookingDate).ToList()
            };

            return View(model);
        }

        // 3. Booking details and tickets
        public ActionResult BookingDetails(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return HttpNotFound();

            var model = new OrganizerBookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                EventTitle = booking.Event.Title,
                EventDate = booking.Event.EventDate,
                VenueName = booking.Event.Venue.VenueName,
                CustomerName = booking.User.CustomerProfile.FullName,
                CustomerEmail = booking.User.Email,
                BookingDate = (DateTime)booking.BookingDate,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                BookingCode = booking.BookingCode,
                TicketCount = booking.Tickets.Count,
                Tickets = booking.Tickets.Select(t => new OrganizerTicketViewModel
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    Category = t.SeatCategory.CategoryName,
                    IsUsed = t.IsUsed ?? false
                }).ToList()
            };

            return View(model);
        }

        // 4. Mark ticket as used
        [HttpPost]
        public ActionResult MarkTicketUsed(int ticketId)
        {
            var ticket = db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null) return HttpNotFound();

            ticket.IsUsed = true;
            db.SaveChanges();

            return Json(new { success = true });
        }

        // 5. Mark ticket as unused
        [HttpPost]
        public ActionResult MarkTicketUnused(int ticketId)
        {
            var ticket = db.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null) return HttpNotFound();

            ticket.IsUsed = false;
            db.SaveChanges();

            return Json(new { success = true });
        }

        // 6. Cancel a booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBooking(int bookingId)
        {
            var booking = db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return HttpNotFound();

            booking.Status = "Canceled";
            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}