using GoCameo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
namespace GoCameo
{
    public class Startup
    {
        //public static string authapiturl = ConfigurationManager.AppSettings["ida:authapiturl"];
        //public static string baseurl = ConfigurationManager.AppSettings["ida:baseurl"];
        public static JObject Menu { get; set; }
        public static System.Collections.Generic.List<JObject> Roles { get; set; }
        public static RolesViewModel selectedRole { get; set; }

        public Startup(IConfiguration configuration)
        {
            selectedRole = new RolesViewModel();
            Menu = new JObject();
            Roles = new System.Collections.Generic.List<JObject>();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddAzureAdB2C(options => { Configuration.Bind("AzureAdB2C", options); AzureAdB2COptions.Settings = options; })
            .AddCookie(options => {
                options.LoginPath = "/Account/Index/";
            });

            services.AddMvc()
            .AddSessionStateTempDataProvider();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
            }); 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Login",
                    template: "{controller=Account}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
