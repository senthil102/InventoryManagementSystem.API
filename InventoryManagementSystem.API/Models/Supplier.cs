using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.API.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(50)]
        public string? State { get; set; }
        
        [StringLength(20)]
        public string? ZipCode { get; set; }
        
        [StringLength(50)]
        public string? Country { get; set; } = "USA";
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(100)]
        public string? ContactPerson { get; set; }
        
        [StringLength(50)]
        public string? TaxId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
} 