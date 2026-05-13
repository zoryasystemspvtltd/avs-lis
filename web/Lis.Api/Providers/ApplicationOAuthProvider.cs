using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
//using System.Web.Script.Serialization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Lis.Api.Models;

namespace Lis.Api.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string publicClientId;
        private Models.IdentityDbContext dbContext;
        private AuthRepository authRepository;

        public ApplicationOAuthProvider(Models.IdentityDbContext dbContext, AuthRepository authRepository, string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            this.publicClientId = publicClientId;
            this.dbContext = dbContext;
            this.authRepository = authRepository;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            ClientApplication client = null;

            ApplicationUser user = await userManager.FindAsync(context.UserName, context.Password);

            if (user == null || user.IsBlocked)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);

            

            var Appid = System.Web.HttpContext.Current.Request.Headers.GetValues("accesskey");
            if (Appid == null || Appid.Count() == 0)
            {
                throw new KeyNotFoundException("Invalid AccessKey specified");
            }

            var clientID = Appid.FirstOrDefault();

            oAuthIdentity.AddClaim(new Claim("modulePermisions", user.GetModulePermission(dbContext, clientID)));

            client = authRepository.FindClient(clientID);

            AuthenticationProperties properties = CreateProperties(user, client);


            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            var originalClient = context.Ticket.Properties.Dictionary["as:client_id"];
            var currentClient = context.ClientId;

            if (originalClient != currentClient)
            {
                context.SetError("invalid_clientId", "Refresh token is issued to a different clientId.");
                return Task.FromResult<object>(null);
            }

            // Change auth ticket for refresh token requests
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);

            var newClaim = newIdentity.Claims.Where(c => c.Type == "newClaim").FirstOrDefault();
            if (newClaim != null)
            {
                newIdentity.RemoveClaim(newClaim);
            }
            newIdentity.AddClaim(new Claim("newClaim", "newValue"));

            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);

            return Task.FromResult<object>(null);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            //// Resource owner password credentials does not provide a client Id.
            //if (context.ClientId == null)
            //{
            //    context.Validated();
            //}

            //return Task.FromResult<object>(null);

            string clientId = string.Empty;
            string clientSecret = string.Empty;
            ClientApplication client = null;

            if (!context.TryGetBasicCredentials(out clientId, out clientSecret))
            {
                context.TryGetFormCredentials(out clientId, out clientSecret);
            }

            if (context.ClientId == null)
            {
                //Remove the comments from the below line context.SetError, and invalidate context 
                //if you want to force sending clientId/secrects once obtain access tokens. 
                context.Validated();
                //context.SetError("invalid_clientId", "ClientId should be sent.");
                return Task.FromResult<object>(null);
            }

            client = authRepository.FindClient(context.ClientId);

            if (client == null)
            {
                context.SetError("invalid_clientId", string.Format("Client '{0}' is not registered in the system.", context.ClientId));
                return Task.FromResult<object>(null);
            }

            context.OwinContext.Set<string>("as:clientAllowedOrigin", client.AllowedOrigin);
            context.OwinContext.Set<string>("as:clientRefreshTokenLifeTime", client.RefreshTokenLifeTime.ToString());

            context.Validated();
            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == publicClientId)
            {
                context.Validated();
                //Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                //if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                //{
                //    context.Validated();
                //}
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(ApplicationUser user, ClientApplication client)
        {

            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", user.UserName},
                { "emailConfirmed", user.EmailConfirmed ? "true" : "false"},
                { "as:client_id", client.AccessKey},
                { "as:clientAllowedOrigin", client.AllowedOrigin},
                { "as:clientRefreshTokenLifeTime", client.RefreshTokenLifeTime.ToString()}
            };

            return new AuthenticationProperties(data);
        }

        
    }
}