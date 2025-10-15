using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class RegisterViewModel
    {
        // Common
        public string Role { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // Customer
        public string FullName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerProfilePhoto { get; set; }
        public DateTime? CustomerDateOfBirth { get; set; }
        public string CustomerGender { get; set; }

        // Organizer
        public string OrganizationName { get; set; }
        public string ContactPerson { get; set; }
        public string OrganizerPhone { get; set; }
        public string OrganizerAddress { get; set; }
        public string Description { get; set; }
        public string OrganizerProfilePhoto { get; set; }

    }
}
