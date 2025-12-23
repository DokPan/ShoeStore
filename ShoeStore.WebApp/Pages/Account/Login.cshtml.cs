/*
   Модель страницы входа в систему
   Обрабатывает аутентификацию пользователей через cookie-based аутентификацию
   Поддерживает тестовые аккаунты для демонстрации функционала
*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ShoeStoreWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(ILogger<LoginModel> logger)
        {
            _logger = logger;
        }

        // Логин пользователя (привязка к форме)
        [BindProperty]
        public string Login { get; set; } = string.Empty;

        // Пароль пользователя (привязка к форме)
        [BindProperty]
        public string Password { get; set; } = string.Empty;

        // URL для возврата после успешного входа
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        // Сообщение об ошибке аутентификации
        public string ErrorMessage { get; set; } = string.Empty;

        // Обработчик GET-запроса для инициализации страницы
        public void OnGet()
        {
            // Установка дефолтного URL возврата, если он не задан
            if (string.IsNullOrEmpty(ReturnUrl))
                ReturnUrl = "/";
        }

        // Обработчик POST-запроса для аутентификации
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Валидация введенных данных
                if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
                {
                    ErrorMessage = "Введите логин и пароль";
                    return Page();
                }

                // Проверка тестового аккаунта администратора
                if (Login == "admin" && Password == "admin")
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Name, "Администратор"),
                        new Claim("Login", "admin"),
                        new Claim("Role", "Администратор")
                    };

                    // Создание identity и аутентификация через cookie
                    var identity = new ClaimsIdentity(claims,
                        CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    // Возврат на предыдущую страницу или главную
                    return LocalRedirect(ReturnUrl);
                }
                // Проверка тестового аккаунта клиента
                else if (Login == "client" && Password == "client")
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "2"),
                        new Claim(ClaimTypes.Name, "Иван Иванов"),
                        new Claim("Login", "client"),
                        new Claim("Role", "Клиент")
                    };

                    var identity = new ClaimsIdentity(claims,
                        CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return LocalRedirect(ReturnUrl);
                }

                // Неправильные учетные данные
                ErrorMessage = "Неверный логин или пароль";
                return Page();
            }
            catch (Exception ex)
            {
                // Логирование ошибки аутентификации
                _logger.LogError(ex, "Ошибка при входе");
                ErrorMessage = "Ошибка при входе в систему";
                return Page();
            }
        }
    }
}