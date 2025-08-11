using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX_Tests.Pages
{
    public class ProductsModelTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_ReturnsSortedAndFilteredProducts()
        {
            // Arrange
            var context = GetDbContext();
            context.Products.AddRange(
                new Product { Name = "Сок", PurchasePrice = 100, Quantity = 5 },
                new Product { Name = "Чипсы", PurchasePrice = 200, Quantity = 10 },
                new Product { Name = "Пицца", PurchasePrice = 500, Quantity = 3 }
            );
            await context.SaveChangesAsync();

            var model = new ProductsModel(context)
            {
                SearchQuery = "Чипсы", // Изменили с "key" на "Чипсы"
                SortColumn = "PurchasePrice",
                SortDescending = true
            };

            // Act
            await model.OnGetAsync();

            // Assert
            Assert.Single(model.Products);
            Assert.Equal("Чипсы", model.Products.First().Name);
        }


        [Fact]
        public async Task OnPostDeleteProductsAsync_DeletesSelectedProducts()
        {
            // Arrange
            var context = GetDbContext();
            context.Products.AddRange(
                new Product { Id = 1, Name = "Сок", PurchasePrice = 100, Quantity = 5 },
                new Product { Id = 2, Name = "Чипсы", PurchasePrice = 200, Quantity = 10 }
            );
            await context.SaveChangesAsync();

            var httpContext = new DefaultHttpContext();
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "SelectedProductIds", "1" }
            });
            httpContext.Request.Form = formCollection;

            var model = new ProductsModel(context)
            {
                SelectedProductIds = new List<int> { 1 }
            };
            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await model.OnPostDeleteProductsAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            Assert.Single(context.Products);
            Assert.DoesNotContain(context.Products, p => p.Id == 1);
        }

        [Fact]
        public async Task OnGetSearchAsync_ReturnsMatchingProducts()
        {
            // Arrange
            var context = GetDbContext();
            context.Products.AddRange(
                new Product { Name = "Пицца", PurchasePrice = 300, SalePrice = 500, Quantity = 2 },
                new Product { Name = "Сок", PurchasePrice = 100, SalePrice = 150, Quantity = 8 }
            );
            await context.SaveChangesAsync();

            var model = new ProductsModel(context);

            // Act
            var result = await model.OnGetSearchAsync("пиц");

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var products = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
            Assert.Single(products);
        }

        [Fact]
        public async Task OnGetAllProducts_ReturnsAllProducts()
        {
            // Arrange
            var context = GetDbContext();
            context.Products.AddRange(
                new Product { Name = "Сок", PurchasePrice = 100, SalePrice = 120, Quantity = 5 },
                new Product { Name = "Чипсы", PurchasePrice = 150, SalePrice = 180, Quantity = 10 }
            );
            await context.SaveChangesAsync();

            var model = new ProductsModel(context);

            // Act
            var result = await model.OnGetAllProducts();

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var products = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
            Assert.Equal(2, products.Count());
        }
    }
}
