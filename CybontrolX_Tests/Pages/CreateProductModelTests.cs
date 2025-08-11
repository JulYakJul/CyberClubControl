using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX_Tests.Pages
{
    public class CreateProductModelTests
    {
        private readonly AppDbContext _context;

        public CreateProductModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task OnPostAddProduct_ValidInput_AddsProductToDatabase()
        {
            // Arrange
            var pageModel = new CreateProductModel(_context)
            {
                NewProduct = new Product
                {
                    Name = "Product 1",
                    PurchasePrice = 100,
                    Quantity = 10
                }
            };

            // Act
            var result = await pageModel.OnPostAddProductAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Products", redirectResult.PageName);
            Assert.Single(_context.Products);
            Assert.Contains(_context.Products, p => p.Name == "Product 1");
            Assert.Contains("Товар успешно добавлен в список товаров.", pageModel.NotificationMessage);
        }

        [Fact]
        public async Task OnPostAddProduct_InvalidInput_DoesNotAddProduct()
        {
            // Arrange
            var pageModel = new CreateProductModel(_context)
            {
                NewProduct = new Product
                {
                    Name = "",
                    PurchasePrice = 100,
                    Quantity = 10
                }
            };

            pageModel.ModelState.AddModelError("NewProduct.Name", "Required");

            // Act
            var result = await pageModel.OnPostAddProductAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Empty(_context.Products);
            Assert.Contains("Ошибка: Некорректные данные.", pageModel.NotificationMessage);
        }
    }
}
