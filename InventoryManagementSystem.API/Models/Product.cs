using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public decimal Cost { get; set; }
        
        [StringLength(100)]
        public string? Category { get; set; }
        
        [StringLength(100)]
        public string? Brand { get; set; }
        
        [StringLength(50)]
        public string? Unit { get; set; }
        
        public int MinimumStockLevel { get; set; }
        
        public int MaximumStockLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    }
} 