using GoCameo.Models;
using GoCameo.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace GoCameo.Server.Business
{
    public class ServiceBL
    {
        ServiceDL _ServiceDL;
        public ServiceBL()
        {
            _ServiceDL = new ServiceDL();
        }
        public async Task<JObject> FetchMenuAsync(string ContentRootPath, List<System.Security.Claims.Claim> roles, string tenant, AuthViewModel auth)
        {
            dynamic menu = null;
            if (tenant == "local")
            {
                menu = GetMetadataAsync("menu", ContentRootPath, tenant);
            }
            else
            {
                menu = await _ServiceDL.GetMenuMetadataAsync(tenant, auth);
            }
            dynamic Menu = Newtonsoft.Json.Linq.JArray.Parse("[]");
            foreach (var nav in menu["Nav"])
            {
                List<string> _roles = nav.roles == null ? new List<string>() : nav.roles.ToObject<List<string>>();
                if (_roles.Count > 0)
                {
                    var res = _roles.FindAll(x => roles.Find(_x => _x.Value == x) != null);
                    if (res != null && res.Count > 0)
                    {
                        Menu.Add(nav);
                    }
                }
                else
                {
                    if (_roles.IndexOf("Tenant Admin") > -1)
                    {
                        Menu.Add(nav);
                    }
                }
            }
            menu["Nav"] = Menu;
            return menu;
        }
        public async Task<object> GetMetadataAsync(string formid, string ContentRootPath, string tenant, AuthViewModel auth = null)
        {
            string FormStr = "";
            object FormObj;
            if (tenant == "local")
            {
                string filename = formid + ".json";
                string filepath = System.IO.Path.Combine(ContentRootPath, "Forms", filename);

                if (System.IO.File.Exists(filepath))
                {
                    FormStr = GetFileData(filename, ContentRootPath);
                }
                FormObj = JsonConvert.DeserializeObject(FormStr);
            }
            else
            {
                FormObj = await _ServiceDL.GetMetadataAsync(formid, tenant, auth);
            }
            return FormObj;
        }
        public async Task<TokenViewModel> LoginAsync(string username, string password)
        {
            TokenViewModel _token = await _ServiceDL.LoginAsync(username, password);
            return _token;
        }
        public static object Find(JArray source, string url)
        {
            foreach (dynamic item in source)
            {
                // dynamic item = source[key];
                // List<string> roles = item.roles == null ? new List<string>() : item.roles.ToObject<List<string>>();
                if (item.Url.ToString() == url)
                    return item;

                // Item not returned yet. Search its children by recursive call.
                if (item.children.Count > 0)
                {
                    var subresult = Find(item.children, url);

                    // If the item was found in the subchildren, return it.
                    if (subresult != null)
                        return subresult;
                }
            }
            // Nothing found yet? return null.
            return null;
        }
        public static StringContent GetContentstring(object data)
        {
            var json = JsonConvert.SerializeObject(data);

            return new StringContent(json, Encoding.UTF8, "application/json");
        }
        public static string GetBaseUrl()
        {
            string BaseUrl = "";
            BaseUrl = AzureAdB2COptions.Settings.baseurl;

            return BaseUrl;
        }


        //****Private Fun*********************///
        private string GetFileData(string fileName, string ContentRootPath)
        {
            StreamReader oRead;
            string filepath = System.IO.Path.Combine(ContentRootPath, "Forms");
            oRead = System.IO.File.OpenText(Path.Combine(filepath, fileName));  //For Local
            string ret = oRead.ReadToEnd();
            oRead.Close();
            return ret;
        }
    }
}
