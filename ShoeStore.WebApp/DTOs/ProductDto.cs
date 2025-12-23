namespace ShoeStoreWeb.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Article { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string CategoryName { get; set; } = null!;
        public string ManufacturerName { get; set; } = null!;
        public string SupplierName { get; set; } = null!;
        public byte[]? ImageData { get; set; }

        public decimal DiscountedPrice => Discount > 0
            ? Price * (1 - Discount / 100)
            : Price;

        public bool IsInStock => StockQuantity > 0;
        public bool HasDiscount => Discount > 0;

        public string ImageUrl => ImageData != null && ImageData.Length > 0
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(ImageData)}"
            : "/images/picture.png";
    }
}