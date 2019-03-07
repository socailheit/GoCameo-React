using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoCameo.Models
{
    public class TokenViewModel
    {
        public int expiresin { get; set; }
        public string accesstoken { get; set; }
        public string refreshtoken { get; set; }
        public string id_token { get; set; }
        public DateTime Expire { get; set; }
    }
}
