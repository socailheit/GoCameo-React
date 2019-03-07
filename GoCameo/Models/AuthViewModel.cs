using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoCameo.Models
{
    public class AuthViewModel
    {

        public int expiresin { get; set; }
        public string accesstoken { get; set; }
        public string refreshToken { get; set; }
        public DateTime accessTokenExpiryDate { get; set; }
        public UserViewModel currentUser { get; set; }
        public bool isSessionExpired { get; set; }
        public string apikey { get; set; }
        public object menu { get; set; }
    }
}
