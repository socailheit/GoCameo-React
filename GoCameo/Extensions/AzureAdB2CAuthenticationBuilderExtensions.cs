using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using GoCameo.Extension;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AzureAdB2CAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder)
            => builder.AddAzureAdB2C(_ => { });

        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder, Action<AzureAdB2COptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureAzureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AzureAdB2COptions _azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdB2COptions> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = _azureOptions.ClientId;
                options.Authority = $"{_azureOptions.Instance}/{_azureOptions.Domain}/{_azureOptions.SignUpSignInPolicyId}/v2.0";
                options.UseTokenLifetime = true;
                options.CallbackPath = _azureOptions.CallbackPath;

                options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "name" };
                // options.UseTokenLifetime = true;
                options.RequireHttpsMetadata = false;
                options.ClientSecret = _azureOptions.ClientSecret;
                // options.Resource = "https://graph.windows.net"; // AAD graph
                options.ResponseType = "id_token code";

                // Subscribing to the OIDC events
                options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceived;
                options.Events.OnTokenValidated = OnTokenValidated;
                options.Events.OnAuthenticationFailed = OnAuthenticationFailed;
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
            public Task OnTokenValidated(TokenValidatedContext context)
            {
                //context.HandleResponse();
                //// Handle the error code that Azure AD B2C throws when trying to reset a password from the login page 
                //// because password reset is not supported by a "sign-up or sign-in policy"
                //if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("AADB2C90118"))
                //{
                //    // If the user clicked the reset password link, redirect to the reset password route
                //    context.Response.Redirect("/Account/ResetPassword");
                //}
                //else if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
                //{
                //    context.Response.Redirect("/");
                //}
                //else
                //{
                //    context.Response.Redirect("/Home/Error");
                //}
                return Task.CompletedTask;
            }
            public Task OnRedirectToIdentityProvider(RedirectContext context)
            {
                var defaultPolicy = _azureOptions.DefaultPolicy;
                if (context.Properties.Items.TryGetValue(AzureAdB2COptions.PolicyAuthenticationProperty, out var policy) &&
                    !policy.Equals(defaultPolicy))
                {
                    context.ProtocolMessage.Scope = OpenIdConnectScope.OpenIdProfile;
                    context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
                    context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.ToLower()
                        .Replace($"/{defaultPolicy.ToLower()}/", $"/{policy.ToLower()}/");
                    context.Properties.Items.Remove(AzureAdB2COptions.PolicyAuthenticationProperty);
                }
                if (context.Request.Path != new Http.PathString("/"))
                {
                    context.Properties.RedirectUri = new Http.PathString("/Account/Index");
                }
                return Task.CompletedTask;
            }

            public Task OnRemoteFailure(RemoteFailureContext context)
            {
                context.HandleResponse();
                // Handle the error code that Azure AD B2C throws when trying to reset a password from the login page 
                // because password reset is not supported by a "sign-up or sign-in policy"
                if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("AADB2C90118"))
                {
                    // If the user clicked the reset password link, redirect to the reset password route
                    context.Response.Redirect("/Account/ResetPassword");
                }
                else if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
                {
                    context.Response.Redirect("/");
                }
                else
                {
                    context.Response.Redirect("/Home/Error");
                }
                return Task.CompletedTask;
            }

            public async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
            {
                // var code = context.ProtocolMessage.Code;
                // Acquire a Token for the Graph API and cache it using ADAL. In the TodoListController, we'll use the cache to acquire a token for the Todo List API
                string userObjectId = (context.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
                var authContext = new AuthenticationContext(context.Options.Authority, new NaiveSessionCache(userObjectId, context.HttpContext.Session));
                var credential = new IdentityModel.Clients.ActiveDirectory.ClientCredential(context.Options.ClientId, context.Options.ClientSecret);

                var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                    new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute), credential, AzureAdB2COptions.Settings.GoCameoResourceId);

                // Notify the OIDC middleware that we already took care of code redemption.
                context.HandleCodeRedemption(authResult.AccessToken, context.ProtocolMessage.IdToken);
            }
            /// <summary>
            /// this method is invoked if exceptions are thrown during request processing
            /// </summary>
            private Task OnAuthenticationFailed(AuthenticationFailedContext context)
            {
                context.HandleResponse();
                context.Response.Redirect("/Home/Error?message=" + context.Exception.Message);
                return Task.FromResult(0);
            }
        }
    }
}
