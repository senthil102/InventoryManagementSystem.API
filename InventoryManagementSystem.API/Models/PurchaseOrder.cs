using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public enum PurchaseOrderStatus
    {
        Draft,
        Submitted,
        Approved,
        Ordered,
        PartiallyReceived,
        Received,
        Cancelled
    }

    public class PurchaseOrder
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        
        public int SupplierId { get; set; }
        
        public int WarehouseId { get; set; }
        
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpectedDeliveryDate { get; set; }
        
        public DateTime? ActualDeliveryDate { get; set; }
        
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public decimal TaxAmount { get; set; }
        
        public decimal ShippingAmount { get; set; }
        
        public decimal GrandTotal => TotalAmount + TaxAmount + ShippingAmount;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
} 