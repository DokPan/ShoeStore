using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.WebApi.DTOs;
using ShoeStoreDb.Data;
using ShoeStoreDb.Models;

namespace ShoeStore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ShoeStoreDbContext _context;

        public ProductsController(ShoeStoreDbContext context)
        {
            _context = context;
        }

        // Получение списка всех товаров - доступно всем пользователям (включая неавторизованных)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    Article = p.Article,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    ManufacturerName = p.Manufacturer != null ? p.Manufacturer.ManufacturerName : null,
                    StockQuantity = p.StockQuantity
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(products);
        }

        // Поиск товара по артикулу - доступно всем пользователям
        [HttpGet("by-article/{article}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDto>> GetProductByArticle(string article)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Where(p => p.Article == article)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    Article = p.Article,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    ManufacturerName = p.Manufacturer != null ? p.Manufacturer.ManufacturerName : null,
                    StockQuantity = p.StockQuantity
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound($"Товар с артикулом {article} не найден");
            }

            return product;
        }

        // Создание нового товара - доступно только администраторам и менеджерам
        [HttpPost]
        [Authorize(Roles = "Администратор,Менеджер")]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Products.AnyAsync(p => p.Article == product.Article))
            {
                return Conflict($"Товар с артикулом {product.Article} уже существует");
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductByArticle),
                new { article = product.Article }, product);
        }

        // Обновление информации о товаре - доступно только администраторам и менеджерам
        [HttpPut("{id}")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest("ID товара не совпадает");
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // Удаление товара - доступно только администраторам и менеджерам
        [HttpDelete("{id}")]
        [Authorize(Roles = "Администратор,Менеджер")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound($"Товар с ID {id} не найден");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.ProductId == id);
        }
    }
}