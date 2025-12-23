using ShoeStoreDb.Models;
using ShoeStoreWeb.DTOs;

namespace ShoeStoreWeb.Service
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetFilteredProductsAsync(
            string? search,
            int? manufacturerId,
            decimal? maxPrice,
            bool onlyWithDiscount,
            bool onlyInStock,
            string sortBy);

        Task<List<Manufacturer>> GetManufacturersAsync();
    }
}