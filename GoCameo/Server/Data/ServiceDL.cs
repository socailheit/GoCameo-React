using GoCameo.Models;
using GoCameo.Server.Business;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GoCameo.Server.Data
{
    public class ServiceDL
    {
        public async Task<TokenViewModel> LoginAsync(string username, string password)
        {
            TokenViewModel _token = new TokenViewModel();
            try
            {
                UriBuilder builder = new UriBuilder(AzureAdB2COptions.Settings.baseurl + "connect/token");
                // string contentstr = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"grant_type\":\"password\",\"scope\":\"offline_access openid email phone profile roles\",\"resource\":\"" + AzureAdB2COptions.Settings.baseurl + "\"}";
                string contentstr = "grant_type=password&username=" + username + "&password=" + password + "&scope=offline_access openid email phone profile roles&resource=" + AzureAdB2COptions.Settings.baseurl;

                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Post, builder.Uri)
                {
                    Content = new StringContent(contentstr, Encoding.UTF8, "application/x-www-form-urlencoded")
                };
                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());
                if (json != null)
                {
                    dynamic tokendata = json;
                    _token.accesstoken = tokendata.access_token;
                    _token.expiresin = tokendata.expires_in;
                    _token.refreshtoken = tokendata.refresh_token;
                    _token.id_token = tokendata.id_token;
                    _token.Expire = DateTime.Now.AddSeconds(_token.expiresin);
                    return _token;
                }
                else
                {
                    return _token;
                }
            }
            catch (Exception e)
            {
                return _token;
            }
        }

        internal async Task<object> GetMetadataAsync(string formid, string tenant, AuthViewModel auth = null)
        {
            try
            {
                UriBuilder builder = new UriBuilder(AzureAdB2COptions.Settings.baseurl + "api/AppData/get");

                builder.Query = "id=" + formid;
                //  var Token = HttpContext.Session.GetString(Utility.SToken);

                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
                if (auth != null)
                {
                    if (!string.IsNullOrEmpty(auth.accesstoken) && !string.IsNullOrEmpty(auth.apikey))
                    {
                        requestUserTimeline.Headers.Add("authorization", "Bearer " + auth.accesstoken);
                        requestUserTimeline.Headers.Add("api-key", auth.apikey);
                    }
                }
                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());

                if (json != null)
                {
                    return json;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal async Task<object> GetMenuMetadataAsync(string tenant, AuthViewModel auth)
        {
            try
            {
                UriBuilder builder = new UriBuilder(AzureAdB2COptions.Settings.baseurl + "api/Application/get");

                builder.Query = "Id=menu&type=menu";
                //  var Token = HttpContext.Session.GetString(Utility.SToken);

                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
                if (!string.IsNullOrEmpty(auth.accesstoken) && !string.IsNullOrEmpty(auth.apikey))
                {
                    requestUserTimeline.Headers.Add("authorization", "Bearer " + auth.accesstoken);
                    requestUserTimeline.Headers.Add("api-key", auth.apikey);
                }

                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline);

                dynamic json = JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());

                if (json != null)
                {
                    return json;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
