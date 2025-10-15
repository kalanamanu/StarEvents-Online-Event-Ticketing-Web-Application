using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class EditEventViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrganizerId { get; set; }
        public string Category { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int VenueId { get; set; }
        public string Status { get; set; }
        public List<int> AvailableVenues { get; set; } // For dropdown
        public List<string> AvailableCategories { get; set; }
    }
}