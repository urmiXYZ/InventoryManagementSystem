using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products.Include(p => p.Category).Include(p => p.Supplier);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToListAsync();

            return Json(products.Select(p => new {
                id = p.Id,
                name = p.Name,
                description = p.Description,
                price = p.Price,
                stockQuantity = p.StockQuantity,
                isActive = p.IsActive,  // 🔹 new property
                categoryName = p.Category != null ? p.Category.Name : "",
                supplierName = p.Supplier != null ? p.Supplier.Name : ""
            }));
        }


        // GET: Products/ByCategory/5
        public async Task<IActionResult> ByCategory(int id)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.CategoryId == id)
                .ToListAsync();

            var category = await _context.Categories.FindAsync(id);
            ViewData["CategoryId"] = id;
            ViewData["CategoryName"] = category?.Name ?? "Unknown";

            return View(products);
        }

        // GET: /Products/GetByCategory/1
        [HttpGet]
        public async Task<IActionResult> GetByCategory(int id)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .ToListAsync();

            return Json(products.Select(p => new {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.StockQuantity,
                CategoryName = p.Category != null ? p.Category.Name : "",
                SupplierName = p.Supplier != null ? p.Supplier.Name : ""
            }));
        }


        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return Json(new { success = false, error = "Product not found" });

            product.IsActive = isActive;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = User.Identity.Name ?? "System";

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return Json(new
            {
                id = product.Id,
                name = product.Name,
                description = product.Description,
                price = product.Price,
                stockQuantity = product.StockQuantity,
                categoryId = product.CategoryId,
                categoryName = product.Category?.Name,
                supplierId = product.SupplierId,
                supplierName = product.Supplier?.Name,
                createdAt = product.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                createdBy = product.CreatedBy,
                updatedAt = product.UpdatedAt?.ToString("yyyy-MM-dd HH:mm"),
                updatedBy = product.UpdatedBy,
                isActive = product.IsActive
            });
        }




        [HttpPost]
        public async Task<IActionResult> CreateOrEdit([FromBody] Product product)
        {
            Product savedProduct;

            if (product.Id > 0)
            {
                var existing = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.Id == product.Id);

                if (existing == null)
                    return Json(new { success = false, error = "Product not found" });

                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryId = product.CategoryId;
                existing.SupplierId = product.SupplierId;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.Identity.Name ?? "System";

                _context.Update(existing);
                savedProduct = existing;
            }
            else
            {
                product.CreatedAt = DateTime.UtcNow;
                product.CreatedBy = User.Identity.Name ?? "System";
                product.IsActive = true;

                _context.Products.Add(product);
                savedProduct = product;
            }

            await _context.SaveChangesAsync();

            // Include Category/Supplier names for JS
            await _context.Entry(savedProduct).Reference(p => p.Category).LoadAsync();
            await _context.Entry(savedProduct).Reference(p => p.Supplier).LoadAsync();

            return Json(new
            {
                success = true,
                product = new
                {
                    savedProduct.Id,
                    savedProduct.Name,
                    savedProduct.Description,
                    savedProduct.Price,
                    savedProduct.StockQuantity,
                    CategoryName = savedProduct.Category?.Name ?? "",
                    SupplierName = savedProduct.Supplier?.Name ?? "",
                    savedProduct.CategoryId,
                    savedProduct.SupplierId
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return Json(new { success = false, error = "Product not found" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
