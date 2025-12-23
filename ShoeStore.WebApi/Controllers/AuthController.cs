using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoeStore.WebApi.DTOs;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShoeStore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ShoeStoreDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ShoeStoreDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Метод входа в систему - проверяет учетные данные и возвращает JWT токен
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Логин и пароль обязательны");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == request.Login);

            if (user == null)
            {
                return Unauthorized("Неверный логин или пароль");
            }

            if (user.Password != request.Password)
            {
                return Unauthorized("Неверный логин или пароль");
            }

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.UserId,
                Login = user.Login,
                FullName = user.FullName,
                Role = user.Role?.RoleName ?? "Клиент"
            });
        }

        // Генерация JWT токена с claims на основе данных пользователя
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            if (string.IsNullOrEmpty(jwtSettings["SecretKey"]))
                throw new InvalidOperationException("JWT SecretKey не настроен");

            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            if (secretKey.Length < 32)
                throw new InvalidOperationException("Секретный ключ должен быть минимум 32 байта");

            var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Login),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.GivenName, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Клиент")
                };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(jwtSettings["ExpiryMinutes"] ?? "60")),
                Issuer = jwtSettings["Issuer"] ?? "ShoeStoreAPI",
                Audience = jwtSettings["Audience"] ?? "ShoeStoreClient",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}