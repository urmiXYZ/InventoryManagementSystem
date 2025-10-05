using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class DeliveriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeliveriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Delivery
        public async Task<IActionResult> Index()
        {
            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .ThenInclude(o => o.Customer)
                .Include(d => d.OrderDetail)
                .ThenInclude(od => od.Product)
                .Where(d => !d.IsDelivered && d.ReturnReason == null) 
                .ToListAsync();

            // Group by OrderId
            var grouped = deliveries.GroupBy(d => d.OrderId)
                                    .Select(g => new DeliveryGroupViewModel
                                    {
                                        OrderId = g.Key,
                                        CustomerName = g.First().Order?.Customer?.Name ?? "",
                                        Products = g.ToList()
                                    }).ToList();

            return View(grouped);
        }

        public async Task<IActionResult> DeliveredIndex()
        {
            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.OrderDetail)
                .Where(d => d.IsDelivered)
                .ToListAsync();

            var model = deliveries
                .GroupBy(d => d.OrderId)
                .Select(g => new DeliveryGroupViewModel
                {
                    OrderId = g.Key,
                    CustomerName = g.First().Order?.Customer?.Name ?? "",
                    Products = g.ToList()
                })
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> ReturnedIndex()
        {
            var deliveries = await _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.OrderDetail)
                .Where(d => d.ReturnReason != null)
                .ToListAsync();

            var model = deliveries
                .GroupBy(d => d.OrderId)
                .Select(g => new DeliveryGroupViewModel
                {
                    OrderId = g.Key,
                    CustomerName = g.First().Order?.Customer?.Name ?? "",
                    Products = g.ToList()
                })
                .ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> MarkDelivered(int orderId, string notes)
        {
            var deliveries = await _context.Deliveries.Where(d => d.OrderId == orderId && !d.IsDelivered && d.ReturnReason == null).ToListAsync(); 
            foreach (var d in deliveries) {
                d.IsDelivered = true; 
                d.DeliveryDate = DateTime.UtcNow; 
                d.Notes = notes; }
            await _context.SaveChangesAsync(); // Update order status if all delivered
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Deliveries)
                .FirstAsync(o => o.Id == orderId);

            // Only mark as delivered if every product has a delivery entry and that entry is delivered
            var allDelivered = order.OrderDetails.All(od =>
                order.Deliveries.Any(d => d.OrderDetailId == od.Id && d.IsDelivered));

            if (allDelivered)
            {
                order.Status = OrderStatus.Delivered;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true }); 
        }

            [HttpPost]
        public async Task<IActionResult> MarkReturned(int orderId, string reason)
        {
            // Fetch all deliveries for this order that are not delivered
            var deliveries = await _context.Deliveries
                .Where(d => d.OrderId == orderId && !d.IsDelivered)
                .ToListAsync();

            if (!deliveries.Any())
                return Json(new { success = false, message = "No undelivered items to return." });

            // Set returned info
            foreach (var d in deliveries)
            {
                d.IsReturned = true;
                d.IsDelivered = false; // ensure not delivered
                d.DeliveryDate = DateTime.UtcNow;
                d.ReturnReason = reason;
            }

            await _context.SaveChangesAsync();

            // Update order status if all items are returned
            var order = await _context.Orders
                .Include(o => o.Deliveries)
                .FirstAsync(o => o.Id == orderId);

            if (order.OrderDetails.All(od =>
                order.Deliveries.Any(d => d.OrderDetailId == od.Id && d.IsReturned)))
            {
                order.Status = OrderStatus.Returned;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }



        [HttpPost]
        public async Task<IActionResult> ReturnDelivery(int orderId, string reason)
        {
            var deliveries = await _context.Deliveries.Where(d => d.OrderId == orderId).ToListAsync();
            foreach (var d in deliveries)
            {
                d.IsDelivered = false; 
                d.DeliveryDate = DateTime.UtcNow;
                d.ReturnReason = reason; 
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // ViewModel for grouping
    public class DeliveryGroupViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public List<Delivery> Products { get; set; }
    }
}

