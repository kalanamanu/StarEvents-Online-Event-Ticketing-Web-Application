using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class EventDiscountViewModel
    {
        public int DiscountId { get; set; }
        public int EventId { get; set; }

        [Required]
        public string DiscountName { get; set; }

        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }

        public string DiscountCode { get; set; }
        public string DiscountType { get; set; }
        public int? MaxUsage { get; set; }
        
        public List<string> SeatCategory { get; set; } // Instead of string SeatCategory
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}