using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StarEvents.Helpers;
using StarEvents.Models;
using StarEvents.ViewModels;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: /Customer/Profile
        public ActionResult Profile()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null) return RedirectToAction("Index", "Home");

            var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
            if (customer == null) return RedirectToAction("Index", "Home");

            // Get all cards from DB
            var cards = db.CustomerCards
                .Where(card => card.CustomerId == customer.CustomerId)
                .Select(card => new CardViewModel
                {
                    CardId = card.CardId,
                    CardNumber = card.CardNumber,
                    CardHolder = card.CardHolder,
                    Expiry = card.Expiry,
                    CVV = null,
                    IsDefault = card.IsDefault
                }).ToList();

            // Mask card numbers in C#
            foreach (var card in cards)
            {
                if (!string.IsNullOrEmpty(card.CardNumber) && card.CardNumber.Length >= 4)
                    card.CardNumber = "**** **** **** " + card.CardNumber.Substring(card.CardNumber.Length - 4);
                else
                    card.CardNumber = "****";
            }

            var model = new CustomerProfileViewModel
            {
                FullName = customer.FullName,
                Email = user.Email,
                Phone = customer.PhoneNumber,
                Address = customer.Address,
                Gender = customer.Gender,
                ProfilePhoto = customer.ProfilePhoto,
                CreatedAt = user.CreatedAt,
                LoyaltyPoints = customer.LoyaltyPoints ?? 0,
                Cards = cards
            };
            return View(model);
        }

        // GET: /Customer/EditProfile
        public ActionResult EditProfile()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null) return RedirectToAction("Index", "Home");
            var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
            if (customer == null) return RedirectToAction("Index", "Home");

            var model = new CustomerProfileViewModel
            {
                FullName = customer.FullName,
                Email = user.Email,
                Phone = customer.PhoneNumber,
                Address = customer.Address,
                Gender = customer.Gender,
                ProfilePhoto = customer.ProfilePhoto,
                CreatedAt = user.CreatedAt,
                LoyaltyPoints = customer.LoyaltyPoints ?? 0
            };
            return View(model);
        }

        // POST: /Customer/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(CustomerProfileViewModel model, HttpPostedFileBase CustomerProfilePhoto)
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null) return RedirectToAction("Index", "Home");
            var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
            if (customer == null) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                customer.FullName = model.FullName;
                customer.PhoneNumber = model.Phone;
                customer.Address = model.Address;
                customer.Gender = model.Gender;

                if (CustomerProfilePhoto != null && CustomerProfilePhoto.ContentLength > 0)
                {
                    string uploadsDir = Server.MapPath("~/uploads/");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    string extension = Path.GetExtension(CustomerProfilePhoto.FileName);
                    string fileName = $"customer_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(uploadsDir, fileName);
                    CustomerProfilePhoto.SaveAs(filePath);
                    customer.ProfilePhoto = "/uploads/" + fileName; 
                }
                db.SaveChanges();
                return RedirectToAction("Profile");
            }
            // If not valid, preserve current photo
            model.ProfilePhoto = customer.ProfilePhoto;
            return View(model);
        }

        // POST: /Customer/AddCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCard(CardViewModel card)
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null) return RedirectToAction("Index", "Home");
            var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
            if (customer == null) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                var newCard = new CustomerCard
                {
                    CustomerId = customer.CustomerId,
                    CardNumber = card.CardNumber,
                    CardHolder = card.CardHolder,
                    Expiry = card.Expiry,
                    CVV = card.CVV,
                    IsDefault = false
                };
                db.CustomerCards.Add(newCard);
                db.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        // POST: /Customer/DeleteCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCard(int cardId)
        {
            var card = db.CustomerCards.Find(cardId);
            if (card != null)
            {
                db.CustomerCards.Remove(card);
                db.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        // POST: /Customer/SetDefaultCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetDefaultCard(int cardId)
        {
            var card = db.CustomerCards.Find(cardId);
            if (card != null)
            {
                var customerCards = db.CustomerCards.Where(c => c.CustomerId == card.CustomerId);
                foreach (var c in customerCards)
                    c.IsDefault = false;
                card.IsDefault = true;
                db.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        // ----------------- SETTINGS PAGE ACTIONS ------------------

        // GET: /Customer/Settings
        public ActionResult Settings()
        {
            return View();
        }

        // POST: /Customer/ChangePassword
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
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
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

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Settings");
        }

        // POST: /Customer/Deactivate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate()
        {
            string email = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
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

        // POST: /Customer/Delete
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
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Settings");
            }

            // Check current password before deleting
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

        private static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return "****";
            return new string('*', cardNumber.Length - 4).PadLeft(cardNumber.Length - 4, '*') + cardNumber.Substring(cardNumber.Length - 4);
        }
    }
}