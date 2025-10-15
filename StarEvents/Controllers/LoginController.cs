using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using StarEvents.Models;
using StarEvents.Helpers;

namespace StarEvents.Controllers
{
    public class LoginController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            // Hash the entered password
            string hashedPassword = PasswordHelper.HashPassword(Password);

            // Check user in database
            var user = db.Users.FirstOrDefault(u => u.Email == Email && u.PasswordHash == hashedPassword && u.IsActive);

            if (user != null)
            {
                // Activity log: login
                db.ActivityLogs.Add(new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    ActivityType = "Login",
                    Description = $"{user.Role} '{user.Username}' logged in.",
                    PerformedBy = user.Username,
                    RelatedEntityId = user.UserId,
                    EntityType = "User"
                });
                db.SaveChanges();

                // Create authentication cookie
                FormsAuthentication.SetAuthCookie(user.Email, false);

                // Store user info in session for navbar/partials
                Session["UserId"] = user.UserId;
                Session["Username"] = user.Username;
                Session["Email"] = user.Email;
                Session["Role"] = user.Role;

                // Fetch profile info for Customer or Organizer (for avatar/full name)
                if (user.Role == "Customer")
                {
                    var customer = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
                    if (customer != null)
                    {
                        Session["FullName"] = customer.FullName;
                        Session["ProfilePhoto"] = customer.ProfilePhoto;
                    }
                }
                else if (user.Role == "Organizer")
                {
                    var org = db.OrganizerProfiles.FirstOrDefault(o => o.OrganizerId == user.UserId);
                    if (org != null)
                    {
                        Session["FullName"] = org.ContactPerson;
                        Session["ProfilePhoto"] = org.ProfilePhoto;
                    }
                }
                else if (user.Role == "Admin")
                {
                    // Admin: fallback/defaults
                    Session["FullName"] = user.Username;
                    Session["ProfilePhoto"] = null;
                }

                // Redirect based on user role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (user.Role == "Organizer")
                {
                    return RedirectToAction("Dashboard", "Organizer");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Invalid login credentials!";
            return View();
        }

        // Logout
        public ActionResult Logout()
        {
            // Clear all session data as well as auth cookie
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }
    }
}