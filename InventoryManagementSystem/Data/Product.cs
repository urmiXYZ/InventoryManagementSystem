using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Data
{
    public class Product : BaseEntity
    {
        public Product()
        {
            OrderDetails = new List<OrderDetails>();
        }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [ForeignKey("Supplier")]
        public int SupplierId { get; set; }
        [ValidateNever]
        public Supplier Supplier { get; set; }
        [ValidateNever]
        public ICollection<OrderDetails> OrderDetails { get; set; }
    }
}
