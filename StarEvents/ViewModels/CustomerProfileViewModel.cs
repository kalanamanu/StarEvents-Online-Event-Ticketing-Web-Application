using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class CustomerProfileViewModel
    {
        // Profile
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string ProfilePhoto { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LoyaltyPoints { get; set; } 

        // Cards
        public List<CardViewModel> Cards { get; set; }
    }

    public class CardViewModel
    {
        public int CardId { get; set; }
        public string CardNumber { get; set; }
        public string CardHolder { get; set; }
        public string Expiry { get; set; }
        public string CVV { get; set; }
        public bool IsDefault { get; set; }
    }
}