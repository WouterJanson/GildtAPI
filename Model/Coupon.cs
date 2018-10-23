using System;

public class Coupon {
    public int couponId {get; set; }
    public string name { get; set; }
    public string Description { get; set; }
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public int type { get; set; }
    public int totalUsed { get; set; }
    public string image { get; set; }
}