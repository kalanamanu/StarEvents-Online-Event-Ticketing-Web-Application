using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StarEvents.Models;

namespace StarEvents.ViewModels
{
    public class PromotionWithSeatPriceViewModel
    {
        public EventDiscount Discount { get; set; }
        public decimal? SeatBasePrice { get; set; }
        public decimal? SeatDiscountedPrice { get; set; }
    }
}