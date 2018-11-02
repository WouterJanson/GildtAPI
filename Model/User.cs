using System.Collections.Generic;

namespace Company.Function
{
    public class User{
        public int userId { get; set; }
        public bool IsAdmin { get; set; }
        public string username { get; set; }
        public string email { get; set;}
        public string password { get; set; }
        public List<UsersCoupon> coupons { get; set; }
    }
}
