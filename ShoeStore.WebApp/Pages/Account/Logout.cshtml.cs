/*
   Модель страницы выхода из системы
   Осуществляет разлогинивание пользователя через очистку cookie аутентификации
*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ShoeStore.WebApp.Pages.Account
{
    public class LogoutModel : PageModel
    {
        // Обработчик POST-запроса для выхода из системы
        public async Task<IActionResult> OnPostAsync()
        {
            // Удаление cookie аутентификации
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Перенаправление на главную страницу
            return RedirectToPage("/Index");
        }
    }
}