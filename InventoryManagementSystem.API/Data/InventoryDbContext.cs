using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.API.Models;

namespace InventoryManagementSystem.API.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<StockAlert> StockAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Warehouse)
                .WithMany()
                .HasForeignKey(po => po.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(poi => poi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAlert>()
                .HasOne(sa => sa.Product)
                .WithMany()
                .HasForeignKey(sa => sa.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockAlert>()
                .HasOne(sa => sa.Warehouse)
                .WithMany()
                .HasForeignKey(sa => sa.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new { i.ProductId, i.WarehouseId })
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(po => po.OrderNumber)
                .IsUnique();

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Warehouses
            modelBuilder.Entity<Warehouse>().HasData(
                new Warehouse
                {
                    Id = 1,
                    Name = "Main Warehouse",
                    Address = "123 Main St",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001",
                    Phone = "555-123-4567",
                    Email = "main@warehouse.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Warehouse
                {
                    Id = 2,
                    Name = "West Coast Warehouse",
                    Address = "456 West Ave",
                    City = "Los Angeles",
                    State = "CA",
                    ZipCode = "90210",
                    Phone = "555-987-6543",
                    Email = "west@warehouse.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed Suppliers
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier
                {
                    Id = 1,
                    Name = "ABC Electronics",
                    Address = "789 Supplier Blvd",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60601",
                    Phone = "555-111-2222",
                    Email = "contact@abcelectronics.com",
                    ContactPerson = "John Smith",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Supplier
                {
                    Id = 2,
                    Name = "XYZ Manufacturing",
                    Address = "321 Factory Rd",
                    City = "Detroit",
                    State = "MI",
                    ZipCode = "48201",
                    Phone = "555-333-4444",
                    Email = "info@xyzmanufacturing.com",
                    ContactPerson = "Jane Doe",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Laptop Computer",
                    SKU = "LAPTOP-001",
                    Description = "High-performance laptop with 16GB RAM",
                    Price = 999.99m,
                    Cost = 750.00m,
                    Category = "Electronics",
                    Brand = "TechCorp",
                    Unit = "Piece",
                    MinimumStockLevel = 10,
                    MaximumStockLevel = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Wireless Mouse",
                    SKU = "MOUSE-001",
                    Description = "Ergonomic wireless mouse",
                    Price = 29.99m,
                    Cost = 15.00m,
                    Category = "Electronics",
                    Brand = "TechCorp",
                    Unit = "Piece",
                    MinimumStockLevel = 50,
                    MaximumStockLevel = 200,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Office Chair",
                    SKU = "CHAIR-001",
                    Description = "Comfortable office chair with lumbar support",
                    Price = 199.99m,
                    Cost = 120.00m,
                    Category = "Furniture",
                    Brand = "ComfortMax",
                    Unit = "Piece",
                    MinimumStockLevel = 5,
                    MaximumStockLevel = 50,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed Inventory
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory
                {
                    Id = 1,
                    ProductId = 1,
                    WarehouseId = 1,
                    Quantity = 25,
                    ReservedQuantity = 5,
                    LastUpdated = DateTime.UtcNow
                },
                new Inventory
                {
                    Id = 2,
                    ProductId = 1,
                    WarehouseId = 2,
                    Quantity = 15,
                    ReservedQuantity = 2,
                    LastUpdated = DateTime.UtcNow
                },
                new Inventory
                {
                    Id = 3,
                    ProductId = 2,
                    WarehouseId = 1,
                    Quantity = 100,
                    ReservedQuantity = 10,
                    LastUpdated = DateTime.UtcNow
                },
                new Inventory
                {
                    Id = 4,
                    ProductId = 3,
                    WarehouseId = 1,
                    Quantity = 8,
                    ReservedQuantity = 1,
                    LastUpdated = DateTime.UtcNow
                }
            );
        }
    }
} 