using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class UserListViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string ProfilePhoto { get; set; }
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        // For Admin
        public string AdminNotes { get; set; }

        // For Customer
        public string CustomerFullName { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string CustomerAddress { get; set; }
        public int? CustomerLoyaltyPoints { get; set; }
        public string CustomerProfilePhoto { get; set; }
        public DateTime? CustomerDateOfBirth { get; set; }
        public string CustomerGender { get; set; }

        // For Organizer
        public string OrganizerOrganizationName { get; set; }
        public string OrganizerContactPerson { get; set; }
        public string OrganizerPhoneNumber { get; set; }
        public string OrganizerAddress { get; set; }
        public string OrganizerDescription { get; set; }
        public string OrganizerProfilePhoto { get; set; }
    }

    public class UserDetailsViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // For Admin
        public string AdminNotes { get; set; }

        // For Customer
        public string CustomerFullName { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string CustomerAddress { get; set; }
        public int? CustomerLoyaltyPoints { get; set; }
        public string CustomerProfilePhoto { get; set; }
        public DateTime? CustomerDateOfBirth { get; set; }
        public string CustomerGender { get; set; }

        // For Organizer
        public string OrganizerOrganizationName { get; set; }
        public string OrganizerContactPerson { get; set; }
        public string OrganizerPhoneNumber { get; set; }
        public string OrganizerAddress { get; set; }
        public string OrganizerDescription { get; set; }
        public string OrganizerProfilePhoto { get; set; }

        public List<UserBookingSummary> BookingHistory { get; set; }
    }

    public class UserBookingSummary
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }
        public DateTime BookingDate { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
    }
}