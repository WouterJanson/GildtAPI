using System.Collections.Generic;
using Newtonsoft.Json;

namespace GildtAPI.Model
{
    public class User{

        [JsonProperty]
        public int Id { get; set; }

        [JsonProperty]
        public bool IsAdmin { get; set; }

        [JsonProperty, JsonRequired]
        public string Username { get; set; }

        [JsonProperty, JsonRequired]
        public string Email { get; set;}

        [JsonProperty, JsonRequired]
        public string Password { get; set; }

        [JsonProperty]
        public List<UsersCoupon> Coupons { get; set; }
    }
}
