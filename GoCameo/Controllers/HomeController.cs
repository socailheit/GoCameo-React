using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GoCameo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.AspNetCore.Authentication;
using GoCameo.Extension;
using Microsoft.AspNetCore.Http;
using GoCameo.Server.Business;

namespace GoCameo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IHostingEnvironment _env;
        private ServiceBL _service;
        public HomeController(IHostingEnvironment env)
        {
            _env = env;
            _service = new ServiceBL();
        }

        public async Task<IActionResult> Index()
        {
            AuthViewModel _AuthStore = HttpContext.Session.GetObject<AuthViewModel>(UtilityViewModel.accesstoken);

            if (User.Identity.IsAuthenticated && _AuthStore != null)
            {
                try
                {
                    if (User.Identity.AuthenticationType == "Passport")
                    {
                        var role = User.Claims.Where(x => x.Type == "role").ToList();
                        dynamic Menu = await _service.FetchMenuAsync(_env.ContentRootPath, role, AzureAdB2COptions.Settings.tenant, _AuthStore);
                        var defaultUrl = Menu.Nav[0].Url.ToString();

                        Startup.Menu = Menu;
                        return RedirectToAction("apps", new { url = defaultUrl });
                    }
                    else
                    {
                        AuthenticationResult result = await GetAzureAuthTokenAsync();
                        // ViewBag.AccessToken = result;
                    }

                    return View();

                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(HomeController.Error), "Home");
                }
            }
            else
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
                // return View("/Account/AccessDenied");
            }
        }

        public async Task<IActionResult> Apps(string url)
        {
            AuthViewModel _AuthStore = HttpContext.Session.GetObject<AuthViewModel>(UtilityViewModel.accesstoken);

            if (_AuthStore != null)
            {
                if (_AuthStore.accessTokenExpiryDate < DateTime.Now)
                {
                    //AuthenticationResult result = await GetAzureAuthTokenAsync();
                    //TokenViewModel Token = await _service.GetAccessTokenByRole(Startup.selectedRole.id, result.AccessToken);
                    //if (Token.accesstoken != null)
                    //{
                    //    // Store application session 
                    //    SetSession(Token);
                    //}
                    //_AuthStore = HttpContext.Session.GetObject<AuthViewModel>(UtilityViewModel.accesstoken);
                    return RedirectToAction(nameof(HomeController.SignOut), "Account");
                }
                dynamic menu = Startup.Menu; // _service.GetMetadata("menu", _env.ContentRootPath);
                dynamic page = JObject.Parse("{}");
                // var formid = "";
                if (string.IsNullOrEmpty(url))
                {
                    url = Convert.ToString(HttpContext.Request.Query["id"]);
                    page = ServiceBL.Find(menu["Nav"], url.Split(",")[0]);
                    //formid = page != null && page.Layout != null && page.Layout.Content.Count > 0 ? page.Layout.Content[0].formid : "";
                    //ViewBag.access = page != null && page.Layout != null && page.Layout.Content.Count > 0 ? page.Layout.Content[0].access.ToString() : "write";
                    _AuthStore.menu = page != null ? page : JObject.Parse("{}");
                }
                else
                {
                    page = ServiceBL.Find(menu["Nav"], url.Split(",")[0]);
                    //formid = page != null && page.Layout != null && page.Layout.Content.Count > 0 ? page.Layout.Content[0].formid : "";
                    //ViewBag.access = page != null && page.Layout != null && page.Layout.Content.Count > 0 ? page.Layout.Content[0].access.ToString() : "write";
                    _AuthStore.menu = page != null ? page : JObject.Parse("{}");
                }
                if (page == null)
                {
                    return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
                }
                else
                {
                    // dynamic formUI = await _service.GetMetadataAsync(formid.ToString(), _env.ContentRootPath, AzureAdB2COptions.Settings.tenant, _AuthStore);

                    // ViewBag.formUI = formUI;
                    ViewData["title"] = "- " + page.Title;
                    ViewBag.settings = JObject.FromObject(_AuthStore);

                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(HomeController.SignOut), "Account");
            }

        }
        public IActionResult DefaultRedirect()
        {
            dynamic Menu = Startup.Menu;
            var defaultUrl = Menu.Count > 0 ? Menu[0].Url.ToString() : "";

            return RedirectToAction("apps", new { url = defaultUrl });
        }
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        async Task<AuthenticationResult> GetAzureAuthTokenAsync()
        {
            AuthenticationResult result;
            // Because we signed-in already in the WebApp, the userObjectId is know
            string userObjectID = (User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;

            // Using ADAL.Net, get a bearer token to access the TodoListService
            AuthenticationContext authContext = new AuthenticationContext(AzureAdB2COptions.Settings.Authority, new NaiveSessionCache(userObjectID, HttpContext.Session));
            ClientCredential credential = new ClientCredential(AzureAdB2COptions.Settings.ClientId, AzureAdB2COptions.Settings.ClientSecret);
            try
            {
                result = await authContext.AcquireTokenSilentAsync(AzureAdB2COptions.Settings.ClientId, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            }
            catch
            {
                result = await authContext.AcquireTokenAsync(AzureAdB2COptions.Settings.ClientId, credential);
            }

            return result;
        }
        //void SetSession(TokenViewModel Token)
        //{
        //    UserViewModel _User = new UserViewModel
        //    {
        //        activerole = Startup.selectedRole.id,
        //        activerolename = Startup.selectedRole.name,
        //        email = User.Claims.Where(x => x.Type == "preferred_username").SingleOrDefault().Value,
        //        Fist_name = User.Identity.Name,
        //        roles = Startup.Roles
        //    };
        //    // preferred_username: kalpak.chavan@collabera.com
        //    AuthViewModel _Auth = new AuthViewModel
        //    {
        //        accesstoken = Token.accesstoken,
        //        accessTokenExpiryDate = Token.Expire,
        //        refreshToken = Token.refreshtoken,
        //        currentUser = _User
        //    };

        //    HttpContext.Session.SetObject(UtilityViewModel.accesstoken, _Auth);
        //}
    }
}
