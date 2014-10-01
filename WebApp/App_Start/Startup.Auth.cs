using Microsoft.AspNet.Identity;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using WebApp.Models;

namespace WebApp
{
    public partial class Startup
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        // The Metadata Address is used by the application to retrieve the signing keys used by Azure AD.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        //
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        public static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        public static string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];
        //private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        public static readonly string Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        public static string graphResourceId = ConfigurationManager.AppSettings["ida:GraphResourceId"];

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
            });

            //Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enable the application to use a cookie to store information for the signed in user
            //app.UseCookieAuthentication(new CookieAuthenticationOptions
            //{
            //    AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            //    LoginPath = new PathString("/Account/Login")
            //});
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            //app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication();
        }
    }
}