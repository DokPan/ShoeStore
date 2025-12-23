/*
   Сервис аутентификации и авторизации пользователей
   Предоставляет методы для проверки учетных данных, создания claims principal
   и получения информации о пользователях из базы данных
*/

using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using System.Security.Claims;

namespace ShoeStoreWeb.Service
{
    public class AuthService : IAuthService
    {
        private readonly ShoeStoreDbContext _context;

        public AuthService(ShoeStoreDbContext context)
        {
            _context = context;
        }

        // Аутентификация пользователя по логину и паролю
        // Возвращает пользователя, если учетные данные верны, иначе null
        public async Task<User> AuthenticateAsync(string login, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)  // Включаем связанную роль для получения полной информации
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password);

            return user;
        }

        // Создание ClaimsPrincipal на основе данных пользователя
        // Используется для создания идентификационных данных в системе аутентификации
        public ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Login", user.Login),
                new Claim("Role", user.Role?.RoleName ?? "Клиент"),
                new Claim("RoleId", user.RoleId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            return new ClaimsPrincipal(identity);
        }

        // Получение пользователя по ID с включением информации о роли
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        // Получение пользователя по логину с включением информации о роли
        public async Task<User> GetUserByLoginAsync(string login)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == login);
        }
    }
}