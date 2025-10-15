using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace StarEvents.Models
{
    public class CreateEventViewModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Category { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime EventDate { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public string VenueName { get; set; } // Changed from VenueId to VenueName (as text)
        public HttpPostedFileBase ImageFile { get; set; }
        public string ImageUrl { get; set; }
        public List<SeatCategoryInputViewModel> SeatCategories { get; set; } = new List<SeatCategoryInputViewModel>();
    }

    public class SeatCategoryInputViewModel
    {
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public int TotalSeats { get; set; }
    }
}