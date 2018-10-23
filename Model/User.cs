using System.Collections.Generic;

namespace Company.Function
{
    public class User{
        public int userId;
        public bool IsAdmin;
        public string username;
        public string email;
        public string password;
        public List<Coupon> coupons;
    }
}
