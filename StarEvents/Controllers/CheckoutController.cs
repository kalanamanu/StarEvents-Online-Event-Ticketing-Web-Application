using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QRCoder;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    public class CheckoutController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /Checkout/Checkout
        [HttpGet]
        public ActionResult Checkout(int EventId, int SeatCategoryId, int Quantity, int? PointsToRedeem)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            var customerProfile = db.CustomerProfiles.FirstOrDefault(p => p.CustomerId == user.UserId);
            var @event = db.Events.FirstOrDefault(e => e.EventId == EventId);
            var seatCategory = db.SeatCategories.FirstOrDefault(sc => sc.SeatCategoryId == SeatCategoryId);

            if (@event == null || seatCategory == null || customerProfile == null)
            {
                ViewBag.Error = "Unable to load booking information.";
                return View(new CheckoutViewModel());
            }

            // Loyalty points calculation
            int earned = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Earn").Sum(lp => (int?)lp.Points) ?? 0;
            int redeemed = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Redeem").Sum(lp => (int?)lp.Points) ?? 0;
            int availablePoints = earned - redeemed;

            // Ticket price and discounts
            decimal pricePerTicket = seatCategory.Price;
            decimal? discounted = null;
            var activeDiscount = @event.EventDiscounts?
                .Where(d => d.IsActive &&
                            (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                            (d.EndDate == null || d.EndDate >= DateTime.Now))
                .OrderByDescending(d => d.DiscountPercent ?? 0)
                .FirstOrDefault();
            if (activeDiscount != null && activeDiscount.DiscountPercent.HasValue)
            {
                discounted = pricePerTicket - (pricePerTicket * activeDiscount.DiscountPercent.Value / 100m);
            }
            decimal finalPrice = discounted ?? pricePerTicket;
            decimal totalPrice = finalPrice * Quantity;

            int pointsUsed = Math.Min(PointsToRedeem ?? 0, availablePoints);
            pointsUsed = Math.Min(pointsUsed, (int)totalPrice);

            var savedCards = db.CustomerCards.Where(c => c.CustomerId == customerProfile.CustomerId).ToList();

            var vm = new CheckoutViewModel
            {
                EventId = @event.EventId,
                EventTitle = @event.Title,
                EventDate = @event.EventDate,
                VenueName = @event.Venue?.VenueName ?? "",
                SeatCategoryId = seatCategory.SeatCategoryId,
                SeatCategoryName = seatCategory.CategoryName,
                Quantity = Quantity,
                PricePerTicket = finalPrice,
                OriginalPricePerTicket = (discounted.HasValue && discounted.Value < pricePerTicket) ? (decimal?)pricePerTicket : null,
                TotalPrice = totalPrice,
                PromotionLabel = activeDiscount != null ? $"{activeDiscount.DiscountName}: {activeDiscount.DiscountPercent.Value:0.#}% off" : null,
                AvailableLoyaltyPoints = availablePoints,
                PointsToRedeem = pointsUsed,
                SavedCards = savedCards.Select(c => new CardSummary
                {
                    CardId = c.CardId,
                    CardHolderName = c.CardHolder,
                    CardNumber = c.CardNumber,
                    Expiry = c.Expiry,
                    IsDefault = c.IsDefault
                }).ToList()
            };
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.Error = TempData["ErrorMessage"];
            }
            return View(vm);
        }

        // POST: /Checkout/Checkout
        [HttpPost]
        public ActionResult Checkout(
            CheckoutViewModel model,
            string newCardNumber,
            string newCardHolder,
            string newCardExpiry,
            string newCardCVV,
            int? selectedCardId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });

            var user = db.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            var customerProfile = db.CustomerProfiles.FirstOrDefault(p => p.CustomerId == user.UserId);
            var @event = db.Events.FirstOrDefault(e => e.EventId == model.EventId);
            var seatCategory = db.SeatCategories.FirstOrDefault(sc => sc.SeatCategoryId == model.SeatCategoryId);

            if (@event == null || seatCategory == null || customerProfile == null)
            {
                TempData["ErrorMessage"] = "Unable to load booking information. Please try again or contact support.";
                return RedirectToAction("Checkout", new
                {
                    EventId = model.EventId,
                    SeatCategoryId = model.SeatCategoryId,
                    Quantity = model.Quantity,
                    PointsToRedeem = model.PointsToRedeem
                });
            }

            int earned = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Earn").Sum(lp => (int?)lp.Points) ?? 0;
            int redeemed = db.LoyaltyPoints.Where(lp => lp.UserId == user.UserId && lp.TransactionType == "Redeem").Sum(lp => (int?)lp.Points) ?? 0;
            int availablePoints = earned - redeemed;

            decimal pricePerTicket = seatCategory.Price;
            decimal? discounted = null;
            var activeDiscount = @event.EventDiscounts?
                .Where(d => d.IsActive &&
                            (d.StartDate == null || d.StartDate <= DateTime.Now) &&
                            (d.EndDate == null || d.EndDate >= DateTime.Now))
                .OrderByDescending(d => d.DiscountPercent ?? 0)
                .FirstOrDefault();
            if (activeDiscount != null && activeDiscount.DiscountPercent.HasValue)
            {
                discounted = pricePerTicket - (pricePerTicket * activeDiscount.DiscountPercent.Value / 100m);
            }
            decimal finalPrice = discounted ?? pricePerTicket;
            decimal totalPrice = finalPrice * model.Quantity;

            int pointsUsed = Math.Min(model.PointsToRedeem, availablePoints);
            pointsUsed = Math.Min(pointsUsed, (int)totalPrice);

            decimal payableAmount = totalPrice - pointsUsed;

            // Payment validation (simulate DB match for saved card or new card)
            bool paymentSuccess = false;
            string paymentMethod = "";
            if (!string.IsNullOrEmpty(newCardNumber) && !string.IsNullOrEmpty(newCardHolder)
                && !string.IsNullOrEmpty(newCardExpiry) && !string.IsNullOrEmpty(newCardCVV))
            {
                var card = db.CustomerCards.FirstOrDefault(c =>
                    c.CardNumber == newCardNumber &&
                    c.CardHolder == newCardHolder &&
                    c.Expiry == newCardExpiry &&
                    c.CVV == newCardCVV &&
                    c.CustomerId == customerProfile.CustomerId);
                paymentSuccess = card != null;
                paymentMethod = "Card";
            }
            else if (selectedCardId.HasValue)
            {
                var card = db.CustomerCards.FirstOrDefault(c => c.CardId == selectedCardId && c.CustomerId == customerProfile.CustomerId);
                paymentSuccess = card != null;
                paymentMethod = "SavedCard";
            }

            if (!paymentSuccess)
            {
                TempData["ErrorMessage"] = "Payment failed: Card not found or details incorrect. Please check your card details or select a saved card.";
                return RedirectToAction("Checkout", new
                {
                    EventId = model.EventId,
                    SeatCategoryId = model.SeatCategoryId,
                    Quantity = model.Quantity,
                    PointsToRedeem = model.PointsToRedeem
                });
            }

            if (seatCategory.AvailableSeats < model.Quantity)
            {
                TempData["ErrorMessage"] = "Not enough seats available. Please reduce the quantity or choose another seat category.";
                return RedirectToAction("Checkout", new
                {
                    EventId = model.EventId,
                    SeatCategoryId = model.SeatCategoryId,
                    Quantity = model.Quantity,
                    PointsToRedeem = model.PointsToRedeem
                });
            }
            seatCategory.AvailableSeats -= model.Quantity;
            db.Entry(seatCategory).State = System.Data.Entity.EntityState.Modified;

            // Add Booking (Status and PaymentId will be set after payment record)
            var booking = new Booking
            {
                EventId = @event.EventId,
                CustomerId = customerProfile.CustomerId,
                SeatCategoryId = seatCategory.SeatCategoryId,
                Quantity = model.Quantity,
                BookingDate = DateTime.Now,
                TotalAmount = payableAmount,
                BookingCode = $"BK-{model.EventId}-{DateTime.Now:yyMMddHHmm}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}",
                Status = "Confirmed" // or "Paid", etc.
                // PaymentId will be assigned after payment is created
            };
            db.Bookings.Add(booking);
            db.SaveChanges();

            //Create Payment record linked to this booking
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                PaidAt = DateTime.Now,
                Amount = payableAmount,
                PaymentMethod = paymentMethod,
                PaymentReference = $"PAY-{booking.BookingId}-{DateTime.Now:yyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                Status = "Paid"
            };
            db.Payments.Add(payment);
            db.SaveChanges();

            //Update Booking with PaymentId
            booking.PaymentId = payment.PaymentId;
            db.Entry(booking).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            // Loyalty: Redeem points
            if (pointsUsed > 0)
            {
                db.LoyaltyPoints.Add(new LoyaltyPoint
                {
                    UserId = user.UserId,
                    TransactionType = "Redeem",
                    Points = pointsUsed,
                    Amount = pointsUsed,
                    Description = $"Redeemed for booking #{booking.BookingId}",
                    CreatedDate = DateTime.Now,
                    RelatedOrderId = booking.BookingId,
                    Status = "Active"
                });
                customerProfile.LoyaltyPoints -= pointsUsed;
            }

            // Loyalty: Earn new points
            int pointsEarned = (int)(payableAmount / 100);
            if (pointsEarned > 0)
            {
                db.LoyaltyPoints.Add(new LoyaltyPoint
                {
                    UserId = user.UserId,
                    TransactionType = "Earn",
                    Points = pointsEarned,
                    Amount = payableAmount,
                    Description = $"Earned for booking #{booking.BookingId}",
                    CreatedDate = DateTime.Now,
                    RelatedOrderId = booking.BookingId,
                    Status = "Active"
                });
                customerProfile.LoyaltyPoints += pointsEarned;
            }
            db.SaveChanges();

            string qrFolder = Server.MapPath("~/Content/QRCodes");
            if (!Directory.Exists(qrFolder))
                Directory.CreateDirectory(qrFolder);

            // Generate QR code for each ticket
            for (int i = 0; i < model.Quantity; i++)
            {
                string qrData = $"Booking:{booking.BookingId};Event:{@event.Title};User:{customerProfile.FullName};Seat:{seatCategory.CategoryName};No:{i + 1}/{model.Quantity}";
                using (var qrGen = new QRCodeGenerator())
                using (var qrDataObj = qrGen.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrDataObj))
                using (var bitmap = qrCode.GetGraphic(20))
                {
                    string qrFileName = $"qr_{booking.BookingId}_{i + 1}_{Guid.NewGuid():N}.png";
                    string qrFilePath = Path.Combine(qrFolder, qrFileName);
                    string qrUrl = "/Content/QRCodes/" + qrFileName;

                    bitmap.Save(qrFilePath, System.Drawing.Imaging.ImageFormat.Png);

                    var ticket = new Ticket
                    {
                        BookingId = booking.BookingId,
                        SeatCategoryId = seatCategory.SeatCategoryId,
                        QRCodePath = qrUrl,
                        TicketCode = $"TKT-{booking.BookingId}-{i + 1}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}",
                        CreatedAt = DateTime.Now,
                        IsUsed = false,
                    };
                    db.Tickets.Add(ticket);
                }
            }
            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = new List<string>();
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
                TempData["ErrorMessage"] = "Ticket creation failed: " + string.Join("; ", errorMessages);
                return RedirectToAction("Checkout", new
                {
                    EventId = model.EventId,
                    SeatCategoryId = model.SeatCategoryId,
                    Quantity = model.Quantity,
                    PointsToRedeem = model.PointsToRedeem
                });
            }

            // Log activity after successful ticket creation
            db.ActivityLogs.Add(new ActivityLog
            {
                Timestamp = DateTime.Now,
                ActivityType = "BookingCreated",
                Description = $"User '{customerProfile.FullName}' booked {model.Quantity} x '{seatCategory.CategoryName}' for event '{@event.Title}'.",
                PerformedBy = customerProfile.FullName,
                RelatedEntityId = booking.BookingId,
                EntityType = "Booking"
            });
            db.SaveChanges();

            // Redirect to the ticket controller
            return RedirectToAction("Eticket", "Ticket", new { bookingId = booking.BookingId });
        }
    }
}