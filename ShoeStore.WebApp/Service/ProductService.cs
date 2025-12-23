/*
   Сервис управления товарами (обувью)
   Предоставляет методы для фильтрации, сортировки и получения информации о товарах
   Включает бизнес-логику работы с каталогом продукции
*/

using Microsoft.EntityFrameworkCore;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;
using ShoeStoreWeb.DTOs;
using ShoeStoreWeb.Service;

namespace ShoeStoreWeb.Services
{
    public class ProductService : IProductService
    {
        private readonly ShoeStoreDbContext _context;

        public ProductService(ShoeStoreDbContext context)
        {
            _context = context;
        }

        // Получение отфильтрованного списка товаров с учетом различных параметров
        public async Task<List<ProductDto>> GetFilteredProductsAsync(
            string? search,           // Поиск по описанию
            int? manufacturerId,      // Фильтр по производителю
            decimal? maxPrice,        // Максимальная цена
            bool onlyWithDiscount,    // Только товары со скидкой
            bool onlyInStock,         // Только товары в наличии
            string sortBy)            // Параметр сортировки
        {
            // Начальный запрос с включением связанных данных
            var query = _context.Products
                .Include(p => p.Category)      // Категория товара
                .Include(p => p.Manufacturer)  // Производитель
                .Include(p => p.Supplier)      // Поставщик
                .AsQueryable();

            // Применение фильтра поиска по описанию
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Description != null &&
                                         p.Description.ToLower().Contains(search.ToLower()));
            }

            // Фильтр по производителю
            if (manufacturerId.HasValue)
            {
                query = query.Where(p => p.ManufacturerId == manufacturerId.Value);
            }

            // Фильтр по максимальной цене
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Фильтр товаров со скидкой
            if (onlyWithDiscount)
            {
                query = query.Where(p => p.Discount > 0);
            }

            // Фильтр товаров в наличии
            if (onlyInStock)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            // Применение сортировки в зависимости от параметра
            query = sortBy switch
            {
                "name" => query.OrderBy(p => p.Name),  // По названию (А-Я)
                "name_desc" => query.OrderByDescending(p => p.Name),  // По названию (Я-А)
                "supplier" => query.OrderBy(p => p.Supplier.SupplierName),  // По поставщику
                "price" => query.OrderBy(p => p.Price),  // По цене (возрастание)
                "price_desc" => query.OrderByDescending(p => p.Price),  // По цене (убывание)
                _ => query.OrderBy(p => p.Name)  // По умолчанию - по названию
            };

            // Проекция данных в DTO для передачи на клиент
            var products = await query.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Article = p.Article,  // Артикул товара
                Name = p.Name,
                Unit = p.Unit,  // Единица измерения
                Price = p.Price,
                Discount = p.Discount,  // Процент скидки
                StockQuantity = p.StockQuantity,  // Количество на складе
                Description = p.Description,
                CategoryName = p.Category.CategoryName,
                ManufacturerName = p.Manufacturer.ManufacturerName,
                SupplierName = p.Supplier.SupplierName,
                ImageData = p.ImageData  // Изображение товара (в байтах)
            }).ToListAsync();

            return products;
        }

        // Получение списка всех производителей для фильтра
        public async Task<List<Manufacturer>> GetManufacturersAsync()
        {
            return await _context.Manufacturers
                .OrderBy(m => m.ManufacturerName)  // Сортировка по названию
                .ToListAsync();
        }
    }
}