using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockAlertsController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockAlertsController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/StockAlerts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockAlert>>> GetStockAlerts()
        {
            return await _context.StockAlerts
                .Include(sa => sa.Product)
                .Include(sa => sa.Warehouse)
                .OrderByDescending(sa => sa.CreatedAt)
                .ToListAsync();
        }

        // GET: api/StockAlerts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockAlert>> GetStockAlert(int id)
        {
            var stockAlert = await _context.StockAlerts
                .Include(sa => sa.Product)
                .Include(sa => sa.Warehouse)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (stockAlert == null)
            {
                return NotFound();
            }

            return stockAlert;
        }

        // GET: api/StockAlerts/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<StockAlert>>> GetActiveAlerts()
        {
            return await _context.StockAlerts
                .Include(sa => sa.Product)
                .Include(sa => sa.Warehouse)
                .Where(sa => sa.Status == AlertStatus.Active)
                .OrderByDescending(sa => sa.CreatedAt)
                .ToListAsync();
        }

        // GET: api/StockAlerts/type/{alertType}
        [HttpGet("type/{alertType}")]
        public async Task<ActionResult<IEnumerable<StockAlert>>> GetAlertsByType(AlertType alertType)
        {
            return await _context.StockAlerts
                .Include(sa => sa.Product)
                .Include(sa => sa.Warehouse)
                .Where(sa => sa.AlertType == alertType)
                .OrderByDescending(sa => sa.CreatedAt)
                .ToListAsync();
        }

        // POST: api/StockAlerts
        [HttpPost]
        public async Task<ActionResult<StockAlert>> CreateStockAlert(StockAlert stockAlert)
        {
            stockAlert.CreatedAt = DateTime.UtcNow;
            stockAlert.Status = AlertStatus.Active;

            _context.StockAlerts.Add(stockAlert);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStockAlert), new { id = stockAlert.Id }, stockAlert);
        }

        // PUT: api/StockAlerts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStockAlert(int id, StockAlert stockAlert)
        {
            if (id != stockAlert.Id)
            {
                return BadRequest();
            }

            var existingAlert = await _context.StockAlerts.FindAsync(id);
            if (existingAlert == null)
            {
                return NotFound();
            }

            existingAlert.Status = stockAlert.Status;
            existingAlert.AcknowledgedAt = stockAlert.AcknowledgedAt;
            existingAlert.ResolvedAt = stockAlert.ResolvedAt;
            existingAlert.AcknowledgedBy = stockAlert.AcknowledgedBy;
            existingAlert.ResolutionNotes = stockAlert.ResolutionNotes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StockAlertExists(id))
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

        // PUT: api/StockAlerts/5/acknowledge
        [HttpPut("{id}/acknowledge")]
        public async Task<IActionResult> AcknowledgeAlert(int id, [FromBody] AlertAcknowledgment acknowledgment)
        {
            var stockAlert = await _context.StockAlerts.FindAsync(id);
            if (stockAlert == null)
            {
                return NotFound();
            }

            stockAlert.Status = AlertStatus.Acknowledged;
            stockAlert.AcknowledgedAt = DateTime.UtcNow;
            stockAlert.AcknowledgedBy = acknowledgment.AcknowledgedBy;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/StockAlerts/5/resolve
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> ResolveAlert(int id, [FromBody] AlertResolution resolution)
        {
            var stockAlert = await _context.StockAlerts.FindAsync(id);
            if (stockAlert == null)
            {
                return NotFound();
            }

            stockAlert.Status = AlertStatus.Resolved;
            stockAlert.ResolvedAt = DateTime.UtcNow;
            stockAlert.ResolutionNotes = resolution.ResolutionNotes;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/StockAlerts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockAlert(int id)
        {
            var stockAlert = await _context.StockAlerts.FindAsync(id);
            if (stockAlert == null)
            {
                return NotFound();
            }

            _context.StockAlerts.Remove(stockAlert);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/StockAlerts/check-low-stock
        [HttpPost("check-low-stock")]
        public async Task<IActionResult> CheckLowStock()
        {
            var lowStockItems = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.AvailableQuantity <= i.Product.MinimumStockLevel)
                .ToListAsync();

            var createdAlerts = new List<StockAlert>();

            foreach (var item in lowStockItems)
            {
                // Check if alert already exists
                var existingAlert = await _context.StockAlerts
                    .FirstOrDefaultAsync(sa => 
                        sa.ProductId == item.ProductId && 
                        sa.WarehouseId == item.WarehouseId && 
                        sa.Status == AlertStatus.Active);

                if (existingAlert == null)
                {
                    var alertType = item.AvailableQuantity == 0 ? AlertType.OutOfStock : AlertType.LowStock;
                    var message = item.AvailableQuantity == 0 
                        ? $"Product {item.Product.Name} is out of stock in {item.Warehouse.Name}"
                        : $"Product {item.Product.Name} is running low on stock in {item.Warehouse.Name}";

                    var stockAlert = new StockAlert
                    {
                        ProductId = item.ProductId,
                        WarehouseId = item.WarehouseId,
                        AlertType = alertType,
                        Status = AlertStatus.Active,
                        Message = message,
                        CurrentStock = item.AvailableQuantity,
                        ThresholdLevel = item.Product.MinimumStockLevel,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.StockAlerts.Add(stockAlert);
                    createdAlerts.Add(stockAlert);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Created {createdAlerts.Count} new stock alerts" });
        }

        // GET: api/StockAlerts/summary
        [HttpGet("summary")]
        public async Task<ActionResult<StockAlertSummary>> GetStockAlertSummary()
        {
            var summary = new StockAlertSummary
            {
                TotalAlerts = await _context.StockAlerts.CountAsync(),
                ActiveAlerts = await _context.StockAlerts.CountAsync(sa => sa.Status == AlertStatus.Active),
                AcknowledgedAlerts = await _context.StockAlerts.CountAsync(sa => sa.Status == AlertStatus.Acknowledged),
                ResolvedAlerts = await _context.StockAlerts.CountAsync(sa => sa.Status == AlertStatus.Resolved),
                LowStockAlerts = await _context.StockAlerts.CountAsync(sa => sa.AlertType == AlertType.LowStock),
                OutOfStockAlerts = await _context.StockAlerts.CountAsync(sa => sa.AlertType == AlertType.OutOfStock)
            };

            return summary;
        }

        private bool StockAlertExists(int id)
        {
            return _context.StockAlerts.Any(e => e.Id == id);
        }
    }

    public class AlertAcknowledgment
    {
        public string AcknowledgedBy { get; set; } = string.Empty;
    }

    public class AlertResolution
    {
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    public class StockAlertSummary
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int LowStockAlerts { get; set; }
        public int OutOfStockAlerts { get; set; }
    }
} 