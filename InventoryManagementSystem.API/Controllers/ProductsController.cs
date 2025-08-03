using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public ProductsController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Inventories)
                .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // GET: api/Products/sku/{sku}
        [HttpGet("sku/{sku}")]
        public async Task<ActionResult<Product>> GetProductBySku(string sku)
        {
            var product = await _context.Products
                .Include(p => p.Inventories)
                .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            // Check if SKU already exists
            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                return BadRequest("SKU already exists");
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null || !existingProduct.IsActive)
            {
                return NotFound();
            }

            // Check if SKU is being changed and if it already exists
            if (existingProduct.SKU != product.SKU && 
                await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                return BadRequest("SKU already exists");
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Cost = product.Cost;
            existingProduct.Category = product.Category;
            existingProduct.Brand = product.Brand;
            existingProduct.Unit = product.Unit;
            existingProduct.MinimumStockLevel = product.MinimumStockLevel;
            existingProduct.MaximumStockLevel = product.MaximumStockLevel;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            return await _context.Products
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // GET: api/Products/brands
        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<string>>> GetBrands()
        {
            return await _context.Products
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id && e.IsActive);
        }
    }
} 