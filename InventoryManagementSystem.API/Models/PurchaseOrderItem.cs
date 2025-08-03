using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        
        public int PurchaseOrderId { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public int ReceivedQuantity { get; set; }
        
        public int PendingQuantity => Quantity - ReceivedQuantity;
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        public decimal TotalPrice => Quantity * UnitPrice;
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
} 