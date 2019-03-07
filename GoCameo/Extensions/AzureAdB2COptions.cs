namespace Microsoft.AspNetCore.Authentication
{
    public class AzureAdB2COptions
    {
        public const string PolicyAuthenticationProperty = "Policy";

        public string ClientId { get; set; }

        public string Instance { get; set; }

        public string Domain { get; set; }

        public string EditProfilePolicyId { get; set; }

        public string SignUpSignInPolicyId { get; set; }

        public string ResetPasswordPolicyId { get; set; }

        public string CallbackPath { get; set; }

        public string DefaultPolicy => SignUpSignInPolicyId;

        public string Authority { get; set; }

        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        public string ApiScopes { get; set; }

        public string tenant { get; set; }

        /// <summary>
        /// Client Id (Application ID) of the TodoListService, obtained from the Azure portal for that application
        /// </summary>
        public string GoCameoResourceId { get; set; }

        /// <summary>
        /// Base URL of the TodoListService
        /// </summary>
        public string authapiturl { get; set; }

        /// <summary>
        /// Base URL of the TodoListService
        /// </summary>
        public string baseurl { get; set; }

        /// <summary>
        /// Instance of the settings for this Web application (to be used in controllers)
        /// </summary>
        public static AzureAdB2COptions Settings { set; get; }
    }
}
