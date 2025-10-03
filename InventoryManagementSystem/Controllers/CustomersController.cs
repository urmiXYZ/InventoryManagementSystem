using InventoryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Customers.ToListAsync());
        }

       

        // GET: /Customers/GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Customers.ToListAsync();
            return Json(customers);
        }

        // GET: /Customers/Get/5
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return Json(customer);
        }


        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Customer customer)
        {
            if (customer == null)
                return BadRequest("Customer object is null");

            customer.CreatedAt = DateTime.Now;
            customer.CreatedBy = User.Identity?.Name ?? "System";
            customer.IsActive = true;

            _context.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(customer);
        }



        // POST: /Customers/EditAjax
        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] Customer customer)
        {
            var existing = await _context.Customers.FindAsync(customer.Id);
            if (existing == null) return NotFound();

            existing.Name = customer.Name;
            existing.Email = customer.Email;
            existing.Mobile = customer.Mobile;
            existing.Address = customer.Address;
            existing.IsActive = customer.IsActive;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = User.Identity?.Name ?? "System";

            _context.Update(existing);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: /Customers/DeleteAjax/5
        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok();
        }


        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
