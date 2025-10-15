using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StarEvents.Models; 

namespace StarEvents.Controllers
{
    public class AccountController : Controller
    {
        private StarEventsDBEntities db = new StarEventsDBEntities();

        [ChildActionOnly]
        public ActionResult UserMenu()
        {
            if (!Request.IsAuthenticated)
                return PartialView("_UserMenu", null);

            // User.Identity.Name contains the email (because we used it in SetAuthCookie)
            string email = User.Identity.Name;

            // Get user and customer profile (or organizer depending on role)
            var user = db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null)
                return PartialView("_UserMenu", null);

            // If user is customer fetch CustomerProfile
            CustomerProfile cust = null;
            if (user.Role == "Customer")
            {
                cust = db.CustomerProfiles.FirstOrDefault(c => c.CustomerId == user.UserId);
            }

            var model = new UserMenuViewModel
            {
                UserId = user.UserId,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                FullName = cust?.FullName ?? user.Username,      // fallback
                ProfilePhoto = cust?.ProfilePhoto                 // may be null
            };

            return PartialView("_UserMenu", model);
        }
    }

    // ViewModel used by the partial view
    public class UserMenuViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public string ProfilePhoto { get; set; } 
    }
}
