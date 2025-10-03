using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Suppliers.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id, [FromBody] bool isActive)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            supplier.IsActive = isActive;
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var suppliers = await _context.Suppliers.ToListAsync();
            return Json(suppliers);
        }


        // GET: Suppliers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suppliers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.UtcNow;
                supplier.CreatedBy = User.Identity.Name ?? "System";
                supplier.IsActive = true;

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                return Json(new { success = true, data = supplier });
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }


        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    supplier.UpdatedAt = DateTime.Now;
                    supplier.UpdatedBy = User.Identity.Name ?? "Default";
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, data = supplier });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                        return Json(new { success = false, error = "Supplier not found" });
                    throw;
                }
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }


        [HttpGet]
        public async Task<IActionResult> CheckSupplierProducts(int id)
        {
            var productCount = await _context.Products.CountAsync(p => p.SupplierId == id);
            return Json(new { productCount });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                    return Json(new { success = false, error = "Supplier not found" });

                var productCount = await _context.Products.CountAsync(p => p.SupplierId == id);
                if (productCount > 0)
                    return Json(new { success = false, error = $"Supplier has {productCount} products" });

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                return Json(new { success = true, name = supplier.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            return Json(new
            {
                supplier.Id,
                supplier.Name,
                supplier.Email,
                supplier.Mobile,
                supplier.Address,
                supplier.CreatedAt,
                supplier.CreatedBy,
                supplier.UpdatedAt,
                supplier.UpdatedBy,
                supplier.IsActive
            });
        }


        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
