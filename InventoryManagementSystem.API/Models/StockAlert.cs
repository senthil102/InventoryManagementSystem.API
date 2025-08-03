using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public enum AlertType
    {
        LowStock,
        OutOfStock,
        ReorderPoint,
        OverStock
    }

    public enum AlertStatus
    {
        Active,
        Acknowledged,
        Resolved
    }

    public class StockAlert
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        public int WarehouseId { get; set; }
        
        public AlertType AlertType { get; set; }
        
        public AlertStatus Status { get; set; } = AlertStatus.Active;
        
        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;
        
        public int CurrentStock { get; set; }
        
        public int ThresholdLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? AcknowledgedAt { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        [StringLength(100)]
        public string? AcknowledgedBy { get; set; }
        
        [StringLength(500)]
        public string? ResolutionNotes { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
} 