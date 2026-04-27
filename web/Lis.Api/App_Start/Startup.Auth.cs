using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Lis.Api.Providers;
using Lis.Api.Models;
using System.Configuration;
using System.Threading.Tasks;
using SimpleInjector;
using Microsoft.Owin.Security.Cookies;

namespace Lis.Api
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app, Container container)
        {
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            app.CreatePerOwinContext(container.GetInstance<ApplicationUserManager>);
          
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                Provider = new ApplicationOAuthProvider(container.GetInstance<Models.IdentityDbContext>(), container.GetInstance<AuthRepository>(), PublicClientId),
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                // In production mode set AllowInsecureHttp = false
                AllowInsecureHttp = true,
                RefreshTokenProvider = new ApplicationRefreshTokenProvider(container.GetInstance<Models.IdentityDbContext>(), container.GetInstance<AuthRepository>())
            };

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);
            //app.UseOAuthAuthorizationServer(OAuthOptions);
            //app.UseOAuthBearerAuthentication(OAuthBearerOptions);
            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            
        }
    }

    public class GlobalConstant
    {
        
    }
}
