using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public InventoryController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventory()
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .OrderBy(i => i.Product.Name)
                .ThenBy(i => i.Warehouse.Name)
                .ToListAsync();
        }

        // GET: api/Inventory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inventory>> GetInventory(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            return inventory;
        }

        // GET: api/Inventory/product/5
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventoryByProduct(int productId)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.Warehouse.Name)
                .ToListAsync();
        }

        // GET: api/Inventory/warehouse/5
        [HttpGet("warehouse/{warehouseId}")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventoryByWarehouse(int warehouseId)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.WarehouseId == warehouseId)
                .OrderBy(i => i.Product.Name)
                .ToListAsync();
        }

        // GET: api/Inventory/low-stock
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetLowStockItems()
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.AvailableQuantity <= i.Product.MinimumStockLevel)
                .OrderBy(i => i.AvailableQuantity)
                .ToListAsync();
        }

        // POST: api/Inventory
        [HttpPost]
        public async Task<ActionResult<Inventory>> CreateInventory(Inventory inventory)
        {
            // Check if inventory already exists for this product and warehouse
            var existingInventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == inventory.ProductId && i.WarehouseId == inventory.WarehouseId);

            if (existingInventory != null)
            {
                return BadRequest("Inventory already exists for this product and warehouse");
            }

            inventory.LastUpdated = DateTime.UtcNow;

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInventory), new { id = inventory.Id }, inventory);
        }

        // PUT: api/Inventory/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventory(int id, Inventory inventory)
        {
            if (id != inventory.Id)
            {
                return BadRequest();
            }

            var existingInventory = await _context.Inventories.FindAsync(id);
            if (existingInventory == null)
            {
                return NotFound();
            }

            existingInventory.Quantity = inventory.Quantity;
            existingInventory.ReservedQuantity = inventory.ReservedQuantity;
            existingInventory.Location = inventory.Location;
            existingInventory.Notes = inventory.Notes;
            existingInventory.LastUpdated = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventoryExists(id))
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

        // POST: api/Inventory/5/adjust
        [HttpPost("{id}/adjust")]
        public async Task<IActionResult> AdjustInventory(int id, [FromBody] InventoryAdjustment adjustment)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            switch (adjustment.Type)
            {
                case "add":
                    inventory.Quantity += adjustment.Quantity;
                    break;
                case "subtract":
                    if (inventory.Quantity < adjustment.Quantity)
                    {
                        return BadRequest("Insufficient stock");
                    }
                    inventory.Quantity -= adjustment.Quantity;
                    break;
                case "reserve":
                    if (inventory.AvailableQuantity < adjustment.Quantity)
                    {
                        return BadRequest("Insufficient available stock");
                    }
                    inventory.ReservedQuantity += adjustment.Quantity;
                    break;
                case "release":
                    if (inventory.ReservedQuantity < adjustment.Quantity)
                    {
                        return BadRequest("Insufficient reserved stock");
                    }
                    inventory.ReservedQuantity -= adjustment.Quantity;
                    break;
                default:
                    return BadRequest("Invalid adjustment type");
            }

            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Inventory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Inventory/summary
        [HttpGet("summary")]
        public async Task<ActionResult<InventorySummary>> GetInventorySummary()
        {
            var summary = new InventorySummary
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalWarehouses = await _context.Warehouses.CountAsync(w => w.IsActive),
                TotalInventoryItems = await _context.Inventories.CountAsync(),
                LowStockItems = await _context.Inventories
                    .Include(i => i.Product)
                    .CountAsync(i => i.AvailableQuantity <= i.Product.MinimumStockLevel),
                OutOfStockItems = await _context.Inventories
                    .CountAsync(i => i.AvailableQuantity == 0),
                TotalValue = await _context.Inventories
                    .Include(i => i.Product)
                    .SumAsync(i => i.Quantity * i.Product.Cost)
            };

            return summary;
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.Id == id);
        }
    }

    public class InventoryAdjustment
    {
        public string Type { get; set; } = string.Empty; // "add", "subtract", "reserve", "release"
        public int Quantity { get; set; }
    }

    public class InventorySummary
    {
        public int TotalProducts { get; set; }
        public int TotalWarehouses { get; set; }
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public decimal TotalValue { get; set; }
    }
} 