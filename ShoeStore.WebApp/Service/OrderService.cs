/*
   Сервис управления заказами
   Обеспечивает создание заказов, управление статусами и получение информации о заказах
   Включает бизнес-логику обработки заказов и взаимодействие с базой данных
*/

using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using ShoeStoreWeb.Service;

namespace ShoeStoreWeb.Services
{
    public class OrderService : IOrderService
    {
        private readonly ShoeStoreDbContext _context;
        private readonly Random _random = new();  // Генератор случайных чисел для кодов получения
        private readonly ILogger<OrderService> _logger;  // Логгер для отслеживания операций

        public OrderService(ShoeStoreDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Создание нового заказа с проверкой доступности товара и обновлением остатков
        public async Task<Order> CreateOrderAsync(int userId, int productId, int quantity)
        {
            try
            {
                _logger.LogInformation($"Создание заказа для UserId={userId}, ProductId={productId}");

                // Проверка существования товара
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    throw new Exception($"Товар с ID {productId} не найден");

                // Проверка наличия достаточного количества товара на складе
                if (product.StockQuantity < quantity)
                    throw new Exception($"Недостаточно товара на складе. В наличии: {product.StockQuantity}");

                // Получение или создание статуса "Новый" для заказа
                var status = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Новый");

                if (status == null)
                {
                    status = new OrderStatus { StatusName = "Новый" };
                    _context.OrderStatuses.Add(status);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Статус ID: {status.StatusId}");

                // Создание объекта заказа
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,  // Дата создания заказа
                    DeliveryDate = DateTime.Now.AddDays(7),  // Дата доставки (через 7 дней)
                    PickupCode = _random.Next(100, 1000),  // Случайный код получения
                    StatusId = status.StatusId  // Установка статуса "Новый"
                };

                // Сохранение заказа в базе данных
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Заказ создан: OrderId={order.OrderId}");

                // Создание элемента заказа (связь заказа с товаром)
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    Quantity = quantity
                };

                _context.OrderItems.Add(orderItem);

                // Обновление количества товара на складе
                product.StockQuantity -= quantity;

                // Сохранение изменений
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Заказ успешно создан: #{order.OrderId}");

                return order;
            }
            catch (DbUpdateException dbEx)
            {
                // Обработка ошибок базы данных
                _logger.LogError(dbEx, "Ошибка базы данных при создании заказа");
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new Exception($"Ошибка базы данных: {innerException}", dbEx);
            }
            catch (Exception ex)
            {
                // Обработка общих ошибок
                _logger.LogError(ex, "Общая ошибка при создании заказа");
                throw;
            }
        }

        // Получение списка заказов для конкретного пользователя
        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Status)  // Включаем информацию о статусе
                    .Include(o => o.OrderItems)  // Включаем элементы заказа
                        .ThenInclude(oi => oi.Product)  // Включаем информацию о товарах
                    .Where(o => o.UserId == userId)  // Фильтр по пользователю
                    .OrderByDescending(o => o.OrderDate)  // Сортировка по дате (новые сначала)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки и возврат пустого списка
                _logger.LogError(ex, "Ошибка при получении заказов пользователя {UserId}", userId);
                return new List<Order>();
            }
        }

        // Получение всех заказов (для администраторов/менеджеров)
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Status)  // Информация о статусе
                    .Include(o => o.User)  // Информация о пользователе
                    .Include(o => o.OrderItems)  // Элементы заказа
                        .ThenInclude(oi => oi.Product)  // Информация о товарах
                    .OrderByDescending(o => o.OrderDate)  // Сортировка по дате
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех заказов");
                return new List<Order>();
            }
        }

        // Получение заказа по ID с полной информацией
        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        // Расчет общей суммы заказа с учетом скидок на товары
        public decimal CalculateOrderTotal(Order order)
        {
            try
            {
                decimal total = 0;

                if (order.OrderItems != null)
                {
                    // Расчет суммы для каждого элемента заказа
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            // Применение скидки к цене товара
                            decimal discount = item.Product.Discount;
                            decimal unitPrice = item.Product.Price * (100 - discount) / 100;
                            total += unitPrice * item.Quantity;
                        }
                    }
                }

                return total;
            }
            catch (Exception ex)
            {
                // Логирование ошибки и возврат нулевой суммы
                _logger.LogError(ex, "Ошибка при расчете суммы заказа {OrderId}", order.OrderId);
                return 0;
            }
        }
    }
}