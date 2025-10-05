namespace InventoryManagementSystem.Data
{
    public class Delivery : BaseEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int OrderDetailId { get; set; }
        public OrderDetails? OrderDetail { get; set; }

        public bool IsDelivered { get; set; } = false; // true - delivered
        public bool IsReturned { get; set; } = false;
        public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;
        public string? ReturnReason { get; set; }
        public string?  Notes { get; set; }

    }
}
