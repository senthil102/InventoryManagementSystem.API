using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SuppliersController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            return await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // GET: api/Suppliers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.PurchaseOrders)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        // POST: api/Suppliers
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
        {
            supplier.CreatedAt = DateTime.UtcNow;
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.IsActive = true;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // PUT: api/Suppliers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return BadRequest();
            }

            var existingSupplier = await _context.Suppliers.FindAsync(id);
            if (existingSupplier == null || !existingSupplier.IsActive)
            {
                return NotFound();
            }

            existingSupplier.Name = supplier.Name;
            existingSupplier.Address = supplier.Address;
            existingSupplier.City = supplier.City;
            existingSupplier.State = supplier.State;
            existingSupplier.ZipCode = supplier.ZipCode;
            existingSupplier.Country = supplier.Country;
            existingSupplier.Phone = supplier.Phone;
            existingSupplier.Email = supplier.Email;
            existingSupplier.ContactPerson = supplier.ContactPerson;
            existingSupplier.TaxId = supplier.TaxId;
            existingSupplier.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(id))
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

        // DELETE: api/Suppliers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null || !supplier.IsActive)
            {
                return NotFound();
            }

            // Check if supplier has purchase orders
            var hasPurchaseOrders = await _context.PurchaseOrders.AnyAsync(po => po.SupplierId == id);
            if (hasPurchaseOrders)
            {
                return BadRequest("Cannot delete supplier with existing purchase orders");
            }

            // Soft delete
            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id && e.IsActive);
        }
    }
} 