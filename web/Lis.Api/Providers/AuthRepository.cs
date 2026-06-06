using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Lis.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Lis.Api.Providers
{
    public class AuthRepository
    {
        private Models.IdentityDbContext dbContext;

        private ApplicationUserManager _userManager;

        public AuthRepository(Models.IdentityDbContext dbContext, ApplicationUserManager userManager)
        {
            this.dbContext = dbContext;
            _userManager = userManager;
        }


        public ClientApplication FindClient(string clientId)
        {
            var client = dbContext.ClientApplications.FirstOrDefault(p => p.AccessKey.Equals(clientId));

            return client;
        }

        public async Task<bool> AddRefreshToken(RefreshToken token)
        {

            var existingToken = dbContext.RefreshTokens.Where(r => r.Subject == token.Subject && r.ClientId == token.ClientId).SingleOrDefault();

            if (existingToken != null)
            {
                var result = await RemoveRefreshToken(existingToken);
            }

            dbContext.RefreshTokens.Add(token);

            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveRefreshToken(string refreshTokenId)
        {
            var refreshToken = await dbContext.RefreshTokens.FindAsync(refreshTokenId);

            if (refreshToken != null)
            {
                dbContext.RefreshTokens.Remove(refreshToken);
                return await dbContext.SaveChangesAsync() > 0;
            }

            return false;
        }

        public async Task<bool> RemoveRefreshToken(RefreshToken refreshToken)
        {
            dbContext.RefreshTokens.Remove(refreshToken);
            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<RefreshToken> FindRefreshToken(string refreshTokenId)
        {
            var refreshToken = await dbContext.RefreshTokens.FindAsync(refreshTokenId);

            return refreshToken;
        }

        public List<RefreshToken> GetAllRefreshTokens()
        {
            return dbContext.RefreshTokens.ToList();
        }

        public async Task<IdentityUser> FindAsync(UserLoginInfo loginInfo)
        {
            IdentityUser user = await _userManager.FindAsync(loginInfo);

            return user;
        }

        public async Task<IdentityResult> CreateAsync(ApplicationUser user)
        {
            var result = await _userManager.CreateAsync(user);

            return result;
        }

        public async Task<IdentityResult> AddLoginAsync(string userId, UserLoginInfo login)
        {
            var result = await _userManager.AddLoginAsync(userId, login);

            return result;
        }

       
    }
}