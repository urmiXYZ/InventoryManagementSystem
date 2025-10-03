using Azure.Core;
using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryManagementSystem.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return Json(new { success = false });

            // Restore stock
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    detail.Product.StockQuantity += detail.Quantity;
                    _context.Products.Update(detail.Product);
                }
            }

            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteDetail(int id)
        {
            var detail = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == id);

            if (detail == null) return Json(new { success = false });

            // Restore stock
            if (detail.Product != null)
            {
                detail.Product.StockQuantity += detail.Quantity;
                _context.Products.Update(detail.Product);
            }

            var order = detail.Order;

            _context.OrderDetails.Remove(detail);
            await _context.SaveChangesAsync();

            // Check if order has any remaining details
            if (order != null)
            {
                var remainingDetails = await _context.OrderDetails
                    .Where(od => od.OrderId == order.Id)
                    .CountAsync();

                if (remainingDetails == 0)
                {
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, orderDeleted = true });
                }
            }

            return Json(new { success = true, orderDeleted = false });
        }



        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.Customers
                .Where(c => c.IsActive)   // only active customers
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Json(customers);
        }


        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)  // only active products
                .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity })
                .ToListAsync();

            return Json(products);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement data)
        {
            try
            {
                if (!data.TryGetProperty("customerId", out var customerIdProp) ||
                    !data.TryGetProperty("status", out var statusProp) ||
                    !data.TryGetProperty("orderDetails", out var detailsProp))
                {
                    return BadRequest("Invalid request data.");
                }

                int customerId = customerIdProp.GetInt32();
                OrderStatus status = (OrderStatus)statusProp.GetInt32();

                // create the order
                var order = new Order
                {
                    CustomerId = customerId,
                    Status = status,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 0m
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // order.Id generated

                decimal totalAmount = 0m;

                foreach (var detailJson in detailsProp.EnumerateArray())
                {
                    int productId = detailJson.GetProperty("productId").GetInt32();
                    int quantity = detailJson.GetProperty("quantity").GetInt32();
                    decimal unitPrice = detailJson.GetProperty("unitPrice").GetDecimal();

                    var product = await _context.Products.FindAsync(productId);
                    if (product == null)
                        return BadRequest($"Product {productId} not found.");

                    if (quantity > product.StockQuantity)
                        return BadRequest($"Not enough stock for {product.Name}");

                    // Subtract stock
                    product.StockQuantity -= quantity;

                    // create order detail (ignore navigation properties)
                    var orderDetail = new OrderDetails
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * quantity
                    };

                    _context.OrderDetails.Add(orderDetail);
                    _context.Products.Update(product);

                    totalAmount += orderDetail.TotalPrice;
                }

                order.TotalAmount = totalAmount;
                _context.Orders.Update(order);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order created successfully!";
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // show exact error for debugging
                return StatusCode(500, ex.ToString());
            }
        }



        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return Json(new { success = false });

            // If order is being cancelled, restore stock
            if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.Product != null)
                    {
                        detail.Product.StockQuantity += detail.Quantity;
                        _context.Products.Update(detail.Product);
                    }
                }
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = "Admin";

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // View page still gets minimal data, no cycles
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Pass only id to view, let AJAX load details
            ViewBag.OrderId = id;
            return View();
        }

        // POST: Orders/Edit
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] OrderDto updatedOrder)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == updatedOrder.Id);

                if (order == null)
                    return Json(new { success = false, error = "Order not found" });

                //  Restore previous stock
                foreach (var od in order.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(od.ProductId);
                    if (prod != null)
                    {
                        prod.StockQuantity += od.Quantity; // add back old quantity
                    }
                }

                // Remove old details
                _context.OrderDetails.RemoveRange(order.OrderDetails);

                //  Add new details and deduct stock
                foreach (var d in updatedOrder.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(d.ProductId);
                    if (prod != null)
                    {
                        if (d.Quantity > prod.StockQuantity)
                            return Json(new { success = false, error = $"Insufficient stock for product {prod.Name}" });

                        prod.StockQuantity -= d.Quantity; // deduct new quantity
                    }

                    _context.OrderDetails.Add(new OrderDetails
                    {
                        OrderId = order.Id,
                        ProductId = d.ProductId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.UnitPrice * d.Quantity
                    });
                }

                // Update order fields
                order.Status = (OrderStatus)updatedOrder.Status;
                order.TotalAmount = updatedOrder.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                order.CustomerId = updatedOrder.CustomerId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = (int)o.Status,
                    CustomerId = o.CustomerId,   // <-- include this
                    CustomerName = o.Customer.Name,
                    OrderDetails = o.OrderDetails.Select(d => new OrderDetailDto
                    {
                        Id = d.Id,
                        ProductName = d.Product.Name,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.TotalPrice,
                        ProductId = d.ProductId     // also include for product selection
                    }).ToList()
                })
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return Json(order);
        }
        [HttpPost]
        public async Task<IActionResult> SendForDelivery([FromBody] SendDeliveryDto data)
        {
            int orderId = data.OrderId;
            List<int> selectedDetailIds = data.ProductDetailIds;

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return Json(new { success = false, error = "Order not found" });

            var selectedDetails = order.OrderDetails.Where(od => selectedDetailIds.Contains(od.Id)).ToList();
            if (!selectedDetails.Any())
                return Json(new { success = false, error = "Select at least one product" });

            int? newOrderId = null;
            decimal remainingTotal = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (selectedDetails.Count == order.OrderDetails.Count)
                {
                    // Whole order  just set InDelivery
                    order.Status = OrderStatus.InDelivery;
                    remainingTotal = order.TotalAmount;
                }
                else
                {
                    // Partial  create new order
                    var newOrder = new Order
                    {
                        CustomerId = order.CustomerId,
                        OrderDate = DateTime.UtcNow,
                        Status = OrderStatus.InDelivery,
                        TotalAmount = selectedDetails.Sum(d => d.TotalPrice),
                        OrderDetails = selectedDetails.Select(d => new OrderDetails
                        {
                            ProductId = d.ProductId,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            TotalPrice = d.TotalPrice
                        }).ToList()
                    };
                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();
                    newOrderId = newOrder.Id;

                    // Remove selected products from original order
                    foreach (var d in selectedDetails)
                    {
                        order.OrderDetails.Remove(d);
                    }
                    remainingTotal = order.OrderDetails.Sum(d => d.TotalPrice);
                }

                order.TotalAmount = remainingTotal;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    newOrderId,
                    newOrder = selectedDetails.Select(d => new
                    {
                        detailId = d.Id,
                        productName = d.Product.Name,
                        quantity = d.Quantity,
                        totalPrice = d.TotalPrice
                    }).ToList(),
                    remainingTotal = order.OrderDetails.Sum(d => d.TotalPrice)
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, error = ex.Message });
            }
        }



    }
    // DTOs

    public class SendDeliveryDto
    {
        public int OrderId { get; set; }
        public List<int> ProductDetailIds { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public int CustomerId { get; set; }       
        public string CustomerName { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; }
    }




    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }     // needed for saving
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
