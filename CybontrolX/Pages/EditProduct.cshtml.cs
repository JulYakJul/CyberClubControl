using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class EditProductModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditProductModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product EditProduct { get; set; }

        public IActionResult OnGet(int id)
        {
            EditProduct = _context.Products.FirstOrDefault(p => p.Id == id);
            if (EditProduct == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var productInDb = _context.Products.FirstOrDefault(p => p.Id == EditProduct.Id);
            if (productInDb == null)
            {
                return NotFound();
            }

            productInDb.Name = EditProduct.Name;
            productInDb.PurchasePrice = EditProduct.PurchasePrice;
            productInDb.SalePrice = EditProduct.SalePrice;
            productInDb.Quantity = EditProduct.Quantity;

            _context.SaveChanges();

            return RedirectToPage("/Products");
        }
    }
}
