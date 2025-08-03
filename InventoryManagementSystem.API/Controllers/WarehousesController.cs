using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public WarehousesController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Warehouses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return await _context.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        // GET: api/Warehouses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.Inventories)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);

            if (warehouse == null)
            {
                return NotFound();
            }

            return warehouse;
        }

        // POST: api/Warehouses
        [HttpPost]
        public async Task<ActionResult<Warehouse>> CreateWarehouse(Warehouse warehouse)
        {
            warehouse.CreatedAt = DateTime.UtcNow;
            warehouse.UpdatedAt = DateTime.UtcNow;
            warehouse.IsActive = true;

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
        }

        // PUT: api/Warehouses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWarehouse(int id, Warehouse warehouse)
        {
            if (id != warehouse.Id)
            {
                return BadRequest();
            }

            var existingWarehouse = await _context.Warehouses.FindAsync(id);
            if (existingWarehouse == null || !existingWarehouse.IsActive)
            {
                return NotFound();
            }

            existingWarehouse.Name = warehouse.Name;
            existingWarehouse.Address = warehouse.Address;
            existingWarehouse.City = warehouse.City;
            existingWarehouse.State = warehouse.State;
            existingWarehouse.ZipCode = warehouse.ZipCode;
            existingWarehouse.Country = warehouse.Country;
            existingWarehouse.Phone = warehouse.Phone;
            existingWarehouse.Email = warehouse.Email;
            existingWarehouse.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WarehouseExists(id))
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

        // DELETE: api/Warehouses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null || !warehouse.IsActive)
            {
                return NotFound();
            }

            // Check if warehouse has inventory
            var hasInventory = await _context.Inventories.AnyAsync(i => i.WarehouseId == id);
            if (hasInventory)
            {
                return BadRequest("Cannot delete warehouse with existing inventory");
            }

            // Soft delete
            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WarehouseExists(int id)
        {
            return _context.Warehouses.Any(e => e.Id == id && e.IsActive);
        }
    }
} 