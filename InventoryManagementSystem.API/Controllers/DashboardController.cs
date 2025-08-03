using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Data;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public DashboardController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/overview
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverview>> GetOverview()
        {
            var overview = new DashboardOverview
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalWarehouses = await _context.Warehouses.CountAsync(w => w.IsActive),
                TotalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive),
                TotalInventoryItems = await _context.Inventories.CountAsync(),
                LowStockItems = await _context.Inventories
                    .Include(i => i.Product)
                    .CountAsync(i => i.AvailableQuantity <= i.Product.MinimumStockLevel),
                OutOfStockItems = await _context.Inventories
                    .CountAsync(i => i.AvailableQuantity == 0),
                ActiveAlerts = await _context.StockAlerts
                    .CountAsync(sa => sa.Status == AlertStatus.Active),
                PendingPurchaseOrders = await _context.PurchaseOrders
                    .CountAsync(po => po.Status == PurchaseOrderStatus.Submitted || 
                                    po.Status == PurchaseOrderStatus.Approved || 
                                    po.Status == PurchaseOrderStatus.Ordered),
                TotalInventoryValue = await _context.Inventories
                    .Include(i => i.Product)
                    .SumAsync(i => i.Quantity * i.Product.Cost)
            };

            return overview;
        }

        // GET: api/Dashboard/inventory-value
        [HttpGet("inventory-value")]
        public async Task<ActionResult<IEnumerable<InventoryValueReport>>> GetInventoryValue()
        {
            var inventoryValue = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .GroupBy(i => new { i.Product.Category, i.Warehouse.Name })
                .Select(g => new InventoryValueReport
                {
                    Category = g.Key.Category ?? "Uncategorized",
                    Warehouse = g.Key.Name,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalValue = g.Sum(i => i.Quantity * i.Product.Cost),
                    AverageCost = g.Average(i => i.Product.Cost)
                })
                .OrderByDescending(r => r.TotalValue)
                .ToListAsync();

            return inventoryValue;
        }

        // GET: api/Dashboard/low-stock-report
        [HttpGet("low-stock-report")]
        public async Task<ActionResult<IEnumerable<LowStockReport>>> GetLowStockReport()
        {
            var lowStockReport = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.AvailableQuantity <= i.Product.MinimumStockLevel)
                .Select(i => new LowStockReport
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    WarehouseName = i.Warehouse.Name,
                    CurrentStock = i.AvailableQuantity,
                    MinimumStock = i.Product.MinimumStockLevel,
                    MaximumStock = i.Product.MaximumStockLevel,
                    UnitCost = i.Product.Cost,
                    StockValue = i.AvailableQuantity * i.Product.Cost,
                    DaysUntilOutOfStock = i.AvailableQuantity == 0 ? 0 : 
                        (int)Math.Ceiling((double)i.AvailableQuantity / 10) // Assuming 10 units per day average usage
                })
                .OrderBy(r => r.CurrentStock)
                .ToListAsync();

            return lowStockReport;
        }

        // GET: api/Dashboard/top-products
        [HttpGet("top-products")]
        public async Task<ActionResult<IEnumerable<TopProductReport>>> GetTopProducts()
        {
            var topProducts = await _context.Inventories
                .Include(i => i.Product)
                .GroupBy(i => i.ProductId)
                .Select(g => new TopProductReport
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    SKU = g.First().Product.SKU,
                    Category = g.First().Product.Category,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalValue = g.Sum(i => i.Quantity * i.Product.Cost),
                    AverageCost = g.Average(i => i.Product.Cost),
                    WarehouseCount = g.Count()
                })
                .OrderByDescending(r => r.TotalValue)
                .Take(10)
                .ToListAsync();

            return topProducts;
        }

        // GET: api/Dashboard/warehouse-summary
        [HttpGet("warehouse-summary")]
        public async Task<ActionResult<IEnumerable<WarehouseSummary>>> GetWarehouseSummary()
        {
            var warehouseSummary = await _context.Warehouses
                .Include(w => w.Inventories)
                .ThenInclude(i => i.Product)
                .Where(w => w.IsActive)
                .Select(w => new WarehouseSummary
                {
                    WarehouseId = w.Id,
                    WarehouseName = w.Name,
                    Location = $"{w.City}, {w.State}",
                    TotalProducts = w.Inventories.Count,
                    TotalQuantity = w.Inventories.Sum(i => i.Quantity),
                    TotalValue = w.Inventories.Sum(i => i.Quantity * i.Product.Cost),
                    LowStockItems = w.Inventories.Count(i => i.AvailableQuantity <= i.Product.MinimumStockLevel),
                    OutOfStockItems = w.Inventories.Count(i => i.AvailableQuantity == 0)
                })
                .OrderByDescending(w => w.TotalValue)
                .ToListAsync();

            return warehouseSummary;
        }

        // GET: api/Dashboard/purchase-order-summary
        [HttpGet("purchase-order-summary")]
        public async Task<ActionResult<PurchaseOrderSummary>> GetPurchaseOrderSummary()
        {
            var totalOrders = await _context.PurchaseOrders.CountAsync();
            var totalValue = await _context.PurchaseOrders.SumAsync(po => po.GrandTotal);
            
            var summary = new PurchaseOrderSummary
            {
                TotalOrders = totalOrders,
                DraftOrders = await _context.PurchaseOrders.CountAsync(po => po.Status == PurchaseOrderStatus.Draft),
                PendingOrders = await _context.PurchaseOrders.CountAsync(po => 
                    po.Status == PurchaseOrderStatus.Submitted || 
                    po.Status == PurchaseOrderStatus.Approved || 
                    po.Status == PurchaseOrderStatus.Ordered),
                ReceivedOrders = await _context.PurchaseOrders.CountAsync(po => po.Status == PurchaseOrderStatus.Received),
                TotalValue = totalValue
            };

            return summary;
        }
    }

    public class DashboardOverview
    {
        public int TotalProducts { get; set; }
        public int TotalWarehouses { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public int ActiveAlerts { get; set; }
        public int PendingPurchaseOrders { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }

    public class InventoryValueReport
    {
        public string Category { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageCost { get; set; }
    }

    public class LowStockReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public int MaximumStock { get; set; }
        public decimal UnitCost { get; set; }
        public decimal StockValue { get; set; }
        public int DaysUntilOutOfStock { get; set; }
    }

    public class TopProductReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageCost { get; set; }
        public int WarehouseCount { get; set; }
    }

    public class WarehouseSummary
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
    }
} 