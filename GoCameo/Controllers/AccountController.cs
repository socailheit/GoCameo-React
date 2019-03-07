using GoCameo.Extension;
using GoCameo.Models;
using GoCameo.Server.Business;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GoCameo.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        ServiceBL _ServiceBL;
        private readonly AzureAdB2COptions _options;

        public AccountController(IOptions<AzureAdB2COptions> b2cOptions)
        {
            _options = b2cOptions.Value;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LoginAsync(IFormCollection frmc)
        {
            /// Extracting the value from FormCollection
            string name = frmc["userName"];
            string pwd = frmc["Password"];

            _ServiceBL = new ServiceBL();
            TokenViewModel _token = await _ServiceBL.LoginAsync(name, pwd);


            if (!string.IsNullOrEmpty(_token.id_token))
            {
                var claimsIdentity = new List<Claim>();
                JwtSecurityTokenHandler hand = new JwtSecurityTokenHandler();

                //read the token as recommended by Coxkie and dpix
                var tokenS = hand.ReadJwtToken(_token.id_token);

                //additional claims needed by your application
                foreach (var claim in tokenS.Claims)
                {
                    claimsIdentity.Add(claim);
                }

                //var claims = new System.Collections.Generic.List<Claim> {
                //    new Claim(ClaimTypes.Name, _token., ClaimValueTypes.String),
                //    new Claim(ClaimTypes.Surname, "Lock", ClaimValueTypes.String),
                //    new Claim(ClaimTypes.Country, "UK", ClaimValueTypes.String),
                //    new Claim("ChildhoodHero", "Ronnie James Dio", ClaimValueTypes.String)
                //};

                var userIdentity = new ClaimsIdentity(claimsIdentity, "Passport");

                var userPrincipal = new ClaimsPrincipal(userIdentity);

                await HttpContext.SignInAsync(userPrincipal);
                //await AuthenticationHttpContextExtensions.SignInAsync(_context, userPrincipal,
                //new AuthenticationProperties
                //{
                //    ExpiresUtc =    System.DateTime.UtcNow.AddMinutes(20),
                //    IsPersistent = false,
                //    AllowRefresh = false
                //});
                SetSession(_token, claimsIdentity);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            else
            {
                ViewBag.Error = "Sorry! Your login attempt was unsuccessful. Please enter correct email and password.";
                return RedirectToAction(nameof(HomeController.Index), "Account");
            }
        }
        [HttpGet]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = _options.ResetPasswordPolicyId;
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = _options.EditProfilePolicyId;
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult SignOut()
        {
            //var callbackUrl = Url.Action(nameof(SignedOut), "Account", values: null, protocol: Request.Scheme);
            //return SignOut(new AuthenticationProperties { RedirectUri = callbackUrl },
            //    CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
            if (User.Identity.AuthenticationType == "Passport")
            {
                HttpContext.SignOutAsync();
                return RedirectToAction(nameof(HomeController.Index), "Account");
            }
            else
            {
                // Remove all cache entries for this user and send an OpenID Connect sign-out request.
                string userObjectID = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var authContext = new AuthenticationContext(AzureAdB2COptions.Settings.Authority,
                                                            new NaiveSessionCache(userObjectID, HttpContext.Session));
                authContext.TokenCache.Clear();

                // Let Azure AD sign-out
                var callbackUrl = Url.Action(nameof(Index), "Account", values: null, protocol: Request.Scheme);
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme);
            }
        }

        [HttpGet]
        public IActionResult SignedOut()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return RedirectToAction(nameof(HomeController.Index), "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        void SetSession(TokenViewModel Token, List<Claim> _Claims)
        {
            UserViewModel _User = new UserViewModel
            {
                activerole = _Claims.Where(x => x.Type == "role").SingleOrDefault().Value,
                activerolename = _Claims.Where(x => x.Type == "role").SingleOrDefault().Value,
                email = _Claims.Where(x => x.Type == "email").SingleOrDefault().Value,
                First_name = _Claims.Where(x => x.Type == "Fist_name").SingleOrDefault().Value,
                Last_name = _Claims.Where(x => x.Type == "Last_name").SingleOrDefault().Value,
                // groups = _Claims.Where(x => x.Type == "groups").ToList(),
                ActiveGroup = _Claims.Where(x => x.Type == "ActiveGroup").SingleOrDefault().Value,
                IndexToken = _Claims.Where(x => x.Type == "IndexToken").SingleOrDefault().Value,
                GroupId = _Claims.Where(x => x.Type == "GroupId").SingleOrDefault().Value,
                // roles = _Claims.Where(x => x.Type == "role").ToList()
            };
            // preferred_username: kalpak.chavan@collabera.com
            AuthViewModel _Auth = new AuthViewModel
            {
                accesstoken = Token.accesstoken,
                accessTokenExpiryDate = Token.Expire,
                refreshToken = Token.refreshtoken,
                apikey = Token.id_token,
                currentUser = _User
            };

            HttpContext.Session.SetObject(UtilityViewModel.accesstoken, _Auth);
        }
    }
}
