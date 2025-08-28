using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Authorization;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Admin, Manager")]
    public class ProductsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProductsModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Product> Products { get; set; } = new List<Product>();

        [BindProperty]
        public List<int> SelectedProductIds { get; set; } = new List<int>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Products
                .Where(p => p.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(p => p.Name.Contains(SearchQuery));
            }

            query = SortColumn switch
            {
                "Name" => SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "PurchasePrice" => SortDescending ? query.OrderByDescending(p => p.PurchasePrice) : query.OrderBy(p => p.PurchasePrice),
                "SalePrice" => SortDescending ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice),
                "Quantity" => SortDescending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
                _ => query.OrderBy(p => p.Name),
            };

            Products = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteProductsAsync()
        {
            if (!string.IsNullOrWhiteSpace(Request.Form["SelectedProductIds"]))
            {
                var selectedIds = Request.Form["SelectedProductIds"]
                    .ToString()
                    .Split(',')
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                Console.WriteLine("Selected Product IDs: " + string.Join(", ", selectedIds));

                var productsToDelete = await _context.Products
                    .Where(p => selectedIds.Contains(p.Id) && p.DeletedAt == null)
                    .ToListAsync();

                if (productsToDelete.Any())
                {
                    foreach (var product in productsToDelete)
                    {
                        product.DeletedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetSearchAsync(string searchQuery)
        {
            var query = _context.Products
                .Where(p => p.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(p => p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            var products = await query
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    purchasePrice = p.PurchasePrice,
                    salePrice = p.SalePrice,
                    quantity = p.Quantity
                })
                .ToListAsync();

            return new JsonResult(products);
        }

        public async Task<IActionResult> OnGetAllProducts()
        {
            var products = await _context.Products
                .Where(p => p.DeletedAt == null)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    purchasePrice = p.PurchasePrice,
                    salePrice = p.SalePrice,
                    quantity = p.Quantity
                })
                .ToListAsync();

            return new JsonResult(products);
        }
    }
}
