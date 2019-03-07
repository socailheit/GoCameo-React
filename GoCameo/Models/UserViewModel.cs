using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoCameo.Models
{
    public class UserViewModel
    {
        public string id { get; set; }
        public string First_name { get; set; }
        public string Last_name { get; set; }
        public string ActiveGroup { get; set; }
        public string IndexToken { get; set; }
        public JObject ActiveApp { get; set; }
        public string GroupId { get; set; }
        public string email { get; set; }
        public List<object> groups { get; set; }
        public string phoneNumber { get; set; }
        public string isEnabled { get; set; }
        public string isLockedOut { get; set; }
        public List<object> roles { get; set; }
        public string activerole { get; set; }
        public string activerolename { get; set; }
        public string Plan { get; set; }
    }
}
