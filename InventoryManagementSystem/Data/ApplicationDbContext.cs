using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Delivery -> Order (disable cascade delete)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Order)
                .WithMany(o => o.Deliveries)  
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Delivery -> OrderDetails (disable cascade delete)
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.OrderDetail)
                .WithMany() 
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
