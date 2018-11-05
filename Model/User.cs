using System.Collections.Generic;

namespace GildtAPI.Model
{
    public class User{
        public int Id { get; set; }
        public bool IsAdmin { get; set; }
        public string Username { get; set; }
        public string Email { get; set;}
        public string Password { get; set; }
        public List<UsersCoupon> Coupons { get; set; }
    }
}
