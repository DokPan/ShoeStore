using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShoeStoreDb.Models;

public partial class Product
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Артикул обязателен")]
    [StringLength(50, ErrorMessage = "Артикул не может быть длиннее 50 символов")]
    public string Article { get; set; } = null!;

    [Required(ErrorMessage = "Название товара обязательно")]
    [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
    public string Name { get; set; } = null!;

    [StringLength(20, ErrorMessage = "Единица измерения не может быть длиннее 20 символов")]
    public string? Unit { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
    public decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Скидка должна быть от 0 до 100%")]
    public decimal Discount { get; set; }

    public int StockQuantity { get; set; }

    public string? Description { get; set; }

    public int SupplierId { get; set; }

    public int ManufacturerId { get; set; }

    public int CategoryId { get; set; }

    [JsonIgnore]
    public byte[]? ImageData { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}