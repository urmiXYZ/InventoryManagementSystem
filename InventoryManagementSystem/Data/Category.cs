using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Data
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public ICollection<Product>? Products { get; set; }

    }
}
