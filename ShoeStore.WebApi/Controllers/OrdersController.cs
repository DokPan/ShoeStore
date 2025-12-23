using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.WebApi.DTOs;
using ShoeStoreDb.Data;
using System.Security.Claims;

namespace ShoeStore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ShoeStoreDbContext _context;

        public OrdersController(ShoeStoreDbContext context)
        {
            _context = context;
        }

        // Получение заказов пользователя с проверкой прав:
        // - Администраторы и менеджеры могут видеть заказы всех пользователей
        // - Обычные пользователи могут видеть только свои заказы
        [HttpGet("by-user/{login}")]
        public async Task<ActionResult<List<OrderDto>>> GetOrdersByUser(string login)
        {
            // Проверяем права
            var currentUserLogin = User.FindFirst(ClaimTypes.Name)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdminOrManager = currentUserRole == "Администратор" ||
                                   currentUserRole == "Менеджер";

            if (!isAdminOrManager && currentUserLogin != login)
            {
                return Forbid("Вы можете просматривать только свои заказы");
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Status)
                .Where(o => o.User.Login == login)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    UserLogin = o.User.Login,
                    OrderDate = o.OrderDate,
                    DeliveryDate = o.DeliveryDate,
                    StatusName = o.Status.StatusName
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(orders);
        }

        // Обновление статуса заказа - доступно только администраторам и менеджерам
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound($"Заказ с ID {id} не найден");
            }

            order.StatusId = request.StatusId;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Статус обновлен" });
        }

        // Обновление даты доставки - доступно только администраторам и менеджерам
        [HttpPut("{id}/delivery-date")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public async Task<IActionResult> UpdateDeliveryDate(int id, [FromBody] UpdateDeliveryDateRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound($"Заказ с ID {id} не найден");
            }

            order.DeliveryDate = request.DeliveryDate;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Дата доставки обновлена" });
        }
    }
}