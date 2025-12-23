namespace ShoeStore.WebApi.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Article { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? CategoryName { get; set; }
        public string? ManufacturerName { get; set; }
        public int StockQuantity { get; set; }
    }
}