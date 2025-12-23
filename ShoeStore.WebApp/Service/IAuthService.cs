using ShoeStoreDb.Models;
using System.Security.Claims;

namespace ShoeStoreWeb.Service
{
    public interface IAuthService
    {
        Task<User> AuthenticateAsync(string login, string password);
        ClaimsPrincipal CreateClaimsPrincipal(User user);
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByLoginAsync(string login);
    }
}