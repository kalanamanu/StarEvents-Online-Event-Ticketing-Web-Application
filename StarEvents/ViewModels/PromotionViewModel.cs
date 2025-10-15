using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string EventTitle { get; set; }
        public string Label { get; set; }      
        public string Type { get; set; }      
        public string Icon { get; set; }       
        public string Description { get; set; }
        public string Color { get; set; }
    }
}