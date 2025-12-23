namespace ShoeStore.WebApi.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string UserLogin { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        public int StatusId { get; set; }
    }

    public class UpdateDeliveryDateRequest
    {
        public DateTime? DeliveryDate { get; set; }
    }
}