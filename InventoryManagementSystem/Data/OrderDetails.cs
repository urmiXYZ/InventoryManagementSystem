using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Data
{
    public class OrderDetails : BaseEntity
    {
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

    }
}
