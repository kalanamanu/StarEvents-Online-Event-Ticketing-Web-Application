using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StarEvents.Models;
using StarEvents.Helpers;
using System.Data.Entity.Validation;

namespace StarEvents.Controllers
{
    public class RegisterController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: Register
        public ActionResult Index()
        {
            // Always send a new model to avoid NullReference in view
            return View(new RegisterViewModel());
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(RegisterViewModel model, HttpPostedFileBase CustomerProfilePhoto, HttpPostedFileBase OrganizerProfilePhoto)
        {
            try
            {
                // Remove validation for unused fields based on role
                if (model.Role == "Customer")
                {
                    ModelState["OrganizationName"]?.Errors.Clear();
                    ModelState["ContactPerson"]?.Errors.Clear();
                    ModelState["OrganizerPhone"]?.Errors.Clear();
                    ModelState["OrganizerAddress"]?.Errors.Clear();
                    ModelState["Description"]?.Errors.Clear();
                    ModelState["ProfilePhoto"]?.Errors.Clear(); // For OrganizerProfile
                }
                else if (model.Role == "Organizer")
                {
                    ModelState["FullName"]?.Errors.Clear();
                    ModelState["CustomerPhone"]?.Errors.Clear();
                    ModelState["CustomerAddress"]?.Errors.Clear();
                    ModelState["ProfilePhoto"]?.Errors.Clear(); // For CustomerProfile
                    ModelState["DateOfBirth"]?.Errors.Clear();
                    ModelState["Gender"]?.Errors.Clear();
                    ModelState["LoyaltyPoints"]?.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    // Check if email exists
                    var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
                    if (existingUser != null)
                    {
                        ViewBag.Error = "Email already registered!";
                        return View(model);
                    }

                    // Check if username exists
                    var usernameUser = db.Users.FirstOrDefault(u => u.Username == model.Username);
                    if (usernameUser != null)
                    {
                        ViewBag.Error = "Username already taken!";
                        return View(model);
                    }

                    // Create user
                    var newUser = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        PasswordHash = PasswordHelper.HashPassword(model.Password),
                        Role = model.Role,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges(); // Save first to get UserId

                    // ---- HANDLE FILE UPLOAD ----
                    string photoUrl = null;
                    if (model.Role == "Customer" && CustomerProfilePhoto != null && CustomerProfilePhoto.ContentLength > 0)
                    {
                        string uploadsDir = Server.MapPath("~/uploads/");
                        if (!Directory.Exists(uploadsDir))
                            Directory.CreateDirectory(uploadsDir);

                        string extension = Path.GetExtension(CustomerProfilePhoto.FileName);
                        string fileName = $"customer_{Guid.NewGuid()}{extension}";
                        string filePath = Path.Combine(uploadsDir, fileName);
                        CustomerProfilePhoto.SaveAs(filePath);
                        photoUrl = "/uploads/" + fileName;
                    }
                    else if (model.Role == "Organizer" && OrganizerProfilePhoto != null && OrganizerProfilePhoto.ContentLength > 0)
                    {
                        string uploadsDir = Server.MapPath("~/uploads/");
                        if (!Directory.Exists(uploadsDir))
                            Directory.CreateDirectory(uploadsDir);

                        string extension = Path.GetExtension(OrganizerProfilePhoto.FileName);
                        string fileName = $"organizer_{Guid.NewGuid()}{extension}";
                        string filePath = Path.Combine(uploadsDir, fileName);
                        OrganizerProfilePhoto.SaveAs(filePath);
                        photoUrl = "/uploads/" + fileName;
                    }

                    // Depending on Role, insert into correct table
                    if (model.Role == "Customer")
                    {
                        var customerProfile = new CustomerProfile
                        {
                            CustomerId = newUser.UserId,
                            FullName = model.FullName,
                            PhoneNumber = model.CustomerPhone,
                            Address = model.CustomerAddress,
                            LoyaltyPoints = 0,
                            ProfilePhoto = photoUrl, // Save URL to DB
                            DateOfBirth = model.CustomerDateOfBirth,
                            Gender = model.CustomerGender
                        };
                        db.CustomerProfiles.Add(customerProfile);
                    }
                    else if (model.Role == "Organizer")
                    {
                        var organizerProfile = new OrganizerProfile
                        {
                            OrganizerId = newUser.UserId,
                            OrganizationName = model.OrganizationName,
                            ContactPerson = model.ContactPerson,
                            PhoneNumber = model.OrganizerPhone,
                            Address = model.OrganizerAddress,
                            Description = model.Description,
                            ProfilePhoto = photoUrl // Save URL to DB
                        };
                        db.OrganizerProfiles.Add(organizerProfile);
                    }

                    db.SaveChanges();
                    TempData["Message"] = "Registration successful! You can now login.";
                    return RedirectToAction("Index", "Login");
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                ViewBag.Error = "Validation error: " + string.Join("; ", errorMessages);
                return View(model);
            }

            // If we reach here, something failed; redisplay form with model and errors
            return View(model);
        }
    }
}