/*
   Модель страницы управления заказами
   Обрабатывает логику отображения, фильтрации и управления заказами
   Разделяет функционал между клиентами и администраторами/менеджерами
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using ShoeStoreWeb.Service;
using System.Security.Claims;

namespace ShoeStoreWeb.Pages.Orders
{
    [Authorize] // Требуется авторизация для доступа к странице
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ShoeStoreDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IOrderService orderService, ShoeStoreDbContext context, ILogger<IndexModel> logger)
        {
            _orderService = orderService;
            _context = context;
            _logger = logger;
        }

        // Список заказов для отображения
        public List<Order> Orders { get; set; } = new();

        // Словарь с итоговыми стоимостями заказов (OrderId -> Total)
        public Dictionary<int, decimal> OrderTotals { get; set; } = new();

        // Флаг, указывающий, является ли пользователь менеджером или администратором
        public bool IsManagerOrAdmin { get; set; }

        // Параметр фильтрации по статусу (доступен только для админов/менеджеров)
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "";

        // Обработчик GET-запроса для загрузки данных страницы
        public async Task OnGetAsync()
        {
            try
            {
                // Получение ID и роли текущего пользователя
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var role = User.FindFirstValue("Role");

                // Определение уровня доступа пользователя
                IsManagerOrAdmin = role == "Менеджер" || role == "Администратор";

                // Загрузка заказов в зависимости от роли пользователя
                if (IsManagerOrAdmin)
                {
                    // Администраторы и менеджеры видят все заказы
                    Orders = await _orderService.GetAllOrdersAsync();
                }
                else
                {
                    // Клиенты видят только свои заказы
                    Orders = await _orderService.GetUserOrdersAsync(userId);
                }

                // Применение фильтра по статусу (если задан)
                if (!string.IsNullOrEmpty(StatusFilter))
                {
                    Orders = Orders
                        .Where(o => o.Status != null && o.Status.StatusName == StatusFilter)
                        .ToList();
                }

                // Расчет итоговой стоимости для каждого заказа
                foreach (var order in Orders)
                {
                    var total = _orderService.CalculateOrderTotal(order);
                    OrderTotals[order.OrderId] = total;
                }
            }
            catch (FormatException)
            {
                // Ошибка парсинга данных пользователя
                TempData["ErrorMessage"] = "Ошибка формата данных пользователя";
            }
            catch (Exception ex)
            {
                // Общая обработка ошибок
                _logger.LogError(ex, "Ошибка при загрузке заказов");
                TempData["ErrorMessage"] = $"Произошла ошибка при загрузке заказов: {ex.Message}";
            }
        }

        // Обработчик обновления статуса заказа (доступен только админам/менеджерам)
        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string statusName)
        {
            try
            {
                // Проверка прав пользователя
                if (!IsUserInRole("Менеджер", "Администратор"))
                {
                    TempData["ErrorMessage"] = "У вас нет прав для изменения статуса заказа";
                    return RedirectToPage();
                }

                // Поиск заказа с включением информации о статусе
                var order = await _context.Orders
                    .Include(o => o.Status)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = $"Заказ #{orderId} не найден";
                    return RedirectToPage();
                }

                // Поиск или создание нового статуса
                var status = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == statusName);

                if (status == null)
                {
                    status = new OrderStatus { StatusName = statusName };
                    _context.OrderStatuses.Add(status);
                    await _context.SaveChangesAsync();
                }

                // Обновление статуса заказа
                order.StatusId = status.StatusId;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Статус заказа #{orderId} изменен на '{statusName}'";
            }
            catch (Exception ex)
            {
                // Логирование ошибки обновления статуса
                _logger.LogError(ex, "Ошибка при изменении статуса заказа {OrderId}", orderId);
                TempData["ErrorMessage"] = $"Ошибка при изменении статуса: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Обработчик обновления даты доставки (доступен только админам/менеджерам)
        public async Task<IActionResult> OnPostUpdateDeliveryDateAsync(int orderId, DateTime deliveryDate)
        {
            try
            {
                // Проверка прав пользователя
                if (!IsUserInRole("Менеджер", "Администратор"))
                {
                    TempData["ErrorMessage"] = "У вас нет прав для изменения даты доставки";
                    return RedirectToPage();
                }

                // Проверка корректности даты (не может быть в прошлом)
                if (deliveryDate.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Дата доставки не может быть в прошлом";
                    return RedirectToPage();
                }

                // Поиск заказа
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["ErrorMessage"] = $"Заказ #{orderId} не найден";
                    return RedirectToPage();
                }

                // Обновление даты доставки
                order.DeliveryDate = deliveryDate;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Дата доставки заказа #{orderId} изменена на {deliveryDate:dd.MM.yyyy}";
            }
            catch (Exception ex)
            {
                // Логирование ошибки обновления даты
                _logger.LogError(ex, "Ошибка при изменении даты доставки заказа {OrderId}", orderId);
                TempData["ErrorMessage"] = $"Ошибка при изменении даты доставки: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Вспомогательный метод для проверки роли пользователя
        private bool IsUserInRole(params string[] roles)
        {
            var userRole = User.FindFirstValue("Role");
            return roles.Contains(userRole);
        }
    }
}