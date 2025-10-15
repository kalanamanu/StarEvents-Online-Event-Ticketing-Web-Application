using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StarEvents.Models;

namespace StarEvents.ViewModels
{
	public class PromotionListViewModel
	{
        public EventDiscount Promotion { get; set; }
        public Event Event { get; set; }
        public SeatCategory SeatCategory { get; set; } 
        public int UsageCount { get; set; }
    }
}