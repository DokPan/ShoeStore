/*
   Модель страницы "Доступ запрещен"
   Простая модель без логики, используется для отображения сообщения об отказе в доступе
*/

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ShoeStoreWeb.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
            // Метод не содержит логики, так как страница только отображает сообщение
        }
    }
}