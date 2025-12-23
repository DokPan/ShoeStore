using ShoeStoreDb.Models;

namespace ShoeStoreWeb.Service
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(int userId, int productId, int quantity);
        Task<List<Order>> GetUserOrdersAsync(int userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderByIdAsync(int orderId);

        decimal CalculateOrderTotal(Order order);
    }
}