using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        public int WarehouseId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public int ReservedQuantity { get; set; }
        
        public int AvailableQuantity => Quantity - ReservedQuantity;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        public string? Location { get; set; }
        
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
} 