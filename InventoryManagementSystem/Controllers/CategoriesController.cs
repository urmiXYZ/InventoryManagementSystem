using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categoties
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();

            return Json(categories.Select(c => new {
                c.Id,
                c.Name,
                c.Description,
                ProductCount = c.Products?.Count() ?? 0
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return Json(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                category.CreatedBy,
                category.UpdatedAt,
                category.UpdatedBy,
                ProductCount = category.Products?.Count() ?? 0,
                Products = category.Products?.Select(p => new
                {
                    p.Id,
                    p.Name
                }).ToList()
            });
        }



        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.UtcNow;
                category.CreatedBy = User.Identity.Name ?? "System";
                category.IsActive = true;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, data = category });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    category.UpdatedAt = DateTime.UtcNow;
                    category.UpdatedBy = User.Identity.Name ?? "System";
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, data = category });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(c => c.Id == category.Id))
                        return Json(new { success = false, error = "Category not found" });
                    throw;
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        

        [HttpPost]
        public IActionResult DeleteAjax(int id)
        {
            var category = _context.Categories.Include(c => c.Products)
                                              .FirstOrDefault(c => c.Id == id);

            if (category == null)
                return Json(new { success = false });

            int deletedProducts = category.Products.Count;

            _context.Products.RemoveRange(category.Products);
            _context.Categories.Remove(category);
            _context.SaveChanges();

            return Json(new { success = true, deletedProducts });
        }
        [HttpPost]
        public IActionResult DeleteWithMove(int id)
        {
            // Include products
            var category = _context.Categories.Include(c => c.Products)
                                              .FirstOrDefault(c => c.Id == id);
            if (category == null)
                return Json(new { success = false, error = "Category not found." });

            int productCount = category.Products.Count;

            if (productCount > 0)
            {
                // Check or create 'Uncategorized'
                var uncategorized = _context.Categories.FirstOrDefault(c => c.Name == "Uncategorized");
                if (uncategorized == null)
                {
                    uncategorized = new Category
                    {
                        Name = "Uncategorized",
                        Description = "Default category for uncategorized products",
                        IsActive = true
                    };
                    _context.Categories.Add(uncategorized);
                    _context.SaveChanges(); // Save to get the Id
                }


                // Move products first
                foreach (var p in category.Products)
                {
                    p.CategoryId = uncategorized.Id;
                    _context.Update(p); // Ensure EF tracks the change
                }

                _context.SaveChanges(); // Save product moves before deleting category
            }

            // Now delete the category
            _context.Categories.Remove(category);
            _context.SaveChanges();

            return Json(new { success = true, movedProducts = productCount });
        }


        [HttpGet]
        public IActionResult CheckCategoryProducts(int id)
        {
            var productCount = _context.Products.Count(p => p.CategoryId == id);
            return Json(new { productCount });
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
