using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public PurchaseOrdersController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/PurchaseOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrders()
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.Items)
                .ThenInclude(item => item.Product)
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();
        }

        // GET: api/PurchaseOrders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrder>> GetPurchaseOrder(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(po => po.Id == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return purchaseOrder;
        }

        // GET: api/PurchaseOrders/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrdersByStatus(PurchaseOrderStatus status)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.Items)
                .ThenInclude(item => item.Product)
                .Where(po => po.Status == status)
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();
        }

        // POST: api/PurchaseOrders
        [HttpPost]
        public async Task<ActionResult<PurchaseOrder>> CreatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            // Generate order number
            purchaseOrder.OrderNumber = await GenerateOrderNumber();
            purchaseOrder.OrderDate = DateTime.UtcNow;
            purchaseOrder.CreatedAt = DateTime.UtcNow;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;
            purchaseOrder.Status = PurchaseOrderStatus.Draft;

            // Calculate totals
            purchaseOrder.TotalAmount = purchaseOrder.Items.Sum(item => item.TotalPrice);

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.Id }, purchaseOrder);
        }

        // PUT: api/PurchaseOrders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchaseOrder(int id, PurchaseOrder purchaseOrder)
        {
            if (id != purchaseOrder.Id)
            {
                return BadRequest();
            }

            var existingOrder = await _context.PurchaseOrders.FindAsync(id);
            if (existingOrder == null)
            {
                return NotFound();
            }

            existingOrder.SupplierId = purchaseOrder.SupplierId;
            existingOrder.WarehouseId = purchaseOrder.WarehouseId;
            existingOrder.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
            existingOrder.Notes = purchaseOrder.Notes;
            existingOrder.TaxAmount = purchaseOrder.TaxAmount;
            existingOrder.ShippingAmount = purchaseOrder.ShippingAmount;
            existingOrder.UpdatedAt = DateTime.UtcNow;

            // Recalculate total
            var items = await _context.PurchaseOrderItems
                .Where(item => item.PurchaseOrderId == id)
                .ToListAsync();
            existingOrder.TotalAmount = items.Sum(item => item.TotalPrice);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderExists(id))
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

        // PUT: api/PurchaseOrders/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePurchaseOrderStatus(int id, [FromBody] PurchaseOrderStatusUpdate statusUpdate)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            purchaseOrder.Status = statusUpdate.Status;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            if (statusUpdate.Status == PurchaseOrderStatus.Received)
            {
                purchaseOrder.ActualDeliveryDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/PurchaseOrders/5/items
        [HttpPost("{id}/items")]
        public async Task<ActionResult<PurchaseOrderItem>> AddPurchaseOrderItem(int id, PurchaseOrderItem item)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            item.PurchaseOrderId = id;
            _context.PurchaseOrderItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPurchaseOrderItem), new { id = item.Id }, item);
        }

        // GET: api/PurchaseOrders/items/5
        [HttpGet("items/{id}")]
        public async Task<ActionResult<PurchaseOrderItem>> GetPurchaseOrderItem(int id)
        {
            var item = await _context.PurchaseOrderItems
                .Include(item => item.Product)
                .Include(item => item.PurchaseOrder)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return item;
        }

        // PUT: api/PurchaseOrders/items/5
        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdatePurchaseOrderItem(int id, PurchaseOrderItem item)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }

            var existingItem = await _context.PurchaseOrderItems.FindAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.ProductId = item.ProductId;
            existingItem.Quantity = item.Quantity;
            existingItem.UnitPrice = item.UnitPrice;
            existingItem.Notes = item.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderItemExists(id))
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

        // DELETE: api/PurchaseOrders/items/5
        [HttpDelete("items/{id}")]
        public async Task<IActionResult> DeletePurchaseOrderItem(int id)
        {
            var item = await _context.PurchaseOrderItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.PurchaseOrderItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/PurchaseOrders/summary
        [HttpGet("summary")]
        public async Task<ActionResult<PurchaseOrderSummary>> GetPurchaseOrderSummary()
        {
            var summary = new PurchaseOrderSummary
            {
                TotalOrders = await _context.PurchaseOrders.CountAsync(),
                DraftOrders = await _context.PurchaseOrders.CountAsync(po => po.Status == PurchaseOrderStatus.Draft),
                PendingOrders = await _context.PurchaseOrders.CountAsync(po => 
                    po.Status == PurchaseOrderStatus.Submitted || 
                    po.Status == PurchaseOrderStatus.Approved || 
                    po.Status == PurchaseOrderStatus.Ordered),
                ReceivedOrders = await _context.PurchaseOrders.CountAsync(po => po.Status == PurchaseOrderStatus.Received),
                TotalValue = await _context.PurchaseOrders.SumAsync(po => po.GrandTotal)
            };

            return summary;
        }

        private async Task<string> GenerateOrderNumber()
        {
            var lastOrder = await _context.PurchaseOrders
                .OrderByDescending(po => po.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastOrder != null)
            {
                var lastNumber = int.Parse(lastOrder.OrderNumber.Replace("PO-", ""));
                nextNumber = lastNumber + 1;
            }

            return $"PO-{nextNumber:D6}";
        }

        private bool PurchaseOrderExists(int id)
        {
            return _context.PurchaseOrders.Any(e => e.Id == id);
        }

        private bool PurchaseOrderItemExists(int id)
        {
            return _context.PurchaseOrderItems.Any(e => e.Id == id);
        }
    }

    public class PurchaseOrderStatusUpdate
    {
        public PurchaseOrderStatus Status { get; set; }
    }

    public class PurchaseOrderSummary
    {
        public int TotalOrders { get; set; }
        public int DraftOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ReceivedOrders { get; set; }
        public decimal TotalValue { get; set; }
    }
} 