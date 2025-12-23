/*
   Класс модели для главной страницы каталога товаров
   Обрабатывает логику фильтрации, сортировки и создания заказов
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShoeStoreDb.Models;
using ShoeStoreWeb.DTOs;
using ShoeStoreWeb.Service;
using System.Security.Claims;

namespace ShoeStoreWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IProductService productService,
            IOrderService orderService,
            ILogger<IndexModel> logger)
        {
            _productService = productService;
            _orderService = orderService;
            _logger = logger;
        }

        // Список отфильтрованных товаров для отображения
        public List<ProductDto> Products { get; set; } = new();

        // Список производителей для выпадающего списка фильтра
        public List<Manufacturer> Manufacturers { get; set; } = new();

        // Свойства для привязки параметров фильтрации из запроса
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public int? ManufacturerId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool OnlyWithDiscount { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool OnlyInStock { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "name";

        // Обработчик GET-запроса для загрузки данных страницы
        public async Task OnGetAsync()
        {
            try
            {
                // Загрузка списка производителей для фильтра
                Manufacturers = await _productService.GetManufacturersAsync();

                // Загрузка отфильтрованных товаров с учетом всех параметров
                Products = await _productService.GetFilteredProductsAsync(
                    search: Search,
                    manufacturerId: ManufacturerId,
                    maxPrice: MaxPrice,
                    onlyWithDiscount: OnlyWithDiscount,
                    onlyInStock: OnlyInStock,
                    sortBy: SortBy);

                _logger.LogInformation($"Загружено товаров: {Products.Count}");
            }
            catch (Exception ex)
            {
                // Обработка ошибок при загрузке данных
                _logger.LogError(ex, "Ошибка при загрузке товаров");
                TempData["ErrorMessage"] = $"Ошибка при загрузке товаров: {ex.Message}";
            }
        }

        // Обработчик POST-запроса для создания заказа
        public async Task<IActionResult> OnPostOrder(int productId)
        {
            // Проверка авторизации пользователя
            if (!User.Identity.IsAuthenticated)
            {
                // Редирект на страницу входа с сохранением текущего URL
                return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/Index") });
            }

            // Проверка роли пользователя - только клиенты могут создавать заказы
            var role = User.FindFirstValue("Role");
            if (role != "Клиент")
            {
                TempData["ErrorMessage"] = "Только клиенты могут создавать заказы";
                return RedirectToPage("/Index");
            }

            try
            {
                // Получение ID пользователя из claims
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation($"Создание заказа: UserId={userId}, ProductId={productId}");

                // Создание заказа через сервис
                var order = await _orderService.CreateOrderAsync(userId, productId, 1);

                // Установка сообщения об успехе
                TempData["SuccessMessage"] = $"Заказ #{order.OrderId} успешно создан! " +
                                           $"Дата доставки: {order.DeliveryDate:dd.MM.yyyy}, " +
                                           $"Код получения: {order.PickupCode}";

                // Редирект на страницу заказов
                return RedirectToPage("/Orders/Index");
            }
            catch (Exception ex)
            {
                // Детальная обработка ошибок при создании заказа
                _logger.LogError(ex, "Ошибка при создании заказа");

                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Внутренняя ошибка: {ex.InnerException.Message}";
                }

                TempData["ErrorMessage"] = $"Ошибка при создании заказа: {errorMessage}";
                return RedirectToPage("/Index");
            }
        }
    }
}