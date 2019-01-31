using System;

namespace GildtAPI.Model
{
    public class Coupon {
        public int Id {get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Type { get; set; }
        public int TotalUsed { get; set; }
        public string Image { get; set; }
    }
}