using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;

namespace CybontrolX_Tests.Pages
{
    public class EditProductModelTests
    {
        private readonly AppDbContext _context;

        public EditProductModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public void OnGet_ProductExists_ReturnsPageResult()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Product1", PurchasePrice = 100, SalePrice = 150, Quantity = 10 };
            _context.Products.Add(product);
            _context.SaveChanges();

            var pageModel = new EditProductModel(_context);

            // Act
            var result = pageModel.OnGet(1);

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Product1", pageModel.EditProduct.Name);
        }

        [Fact]
        public void OnGet_ProductNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditProductModel(_context);

            // Act
            var result = pageModel.OnGet(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void OnPost_ValidProduct_UpdatesProductAndRedirects()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Product1", PurchasePrice = 100, SalePrice = 150, Quantity = 10 };
            _context.Products.Add(product);
            _context.SaveChanges();

            var pageModel = new EditProductModel(_context)
            {
                EditProduct = new Product
                {
                    Id = 1,
                    Name = "Updated Product",
                    PurchasePrice = 120,
                    SalePrice = 170,
                    Quantity = 15
                }
            };

            // Act
            var result = pageModel.OnPost();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Products", redirectResult.PageName);

            var updatedProduct = _context.Products.Find(1);
            Assert.Equal("Updated Product", updatedProduct.Name);
            Assert.Equal(120, updatedProduct.PurchasePrice);
            Assert.Equal(170, updatedProduct.SalePrice);
            Assert.Equal(15, updatedProduct.Quantity);
        }

        [Fact]
        public void OnPost_InvalidProduct_ReturnsPageResultWithValidationErrors()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Product1", PurchasePrice = 100, SalePrice = 150, Quantity = 10 };
            _context.Products.Add(product);
            _context.SaveChanges();

            var pageModel = new EditProductModel(_context)
            {
                EditProduct = new Product { Id = 1 }
            };

            pageModel.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = pageModel.OnPost();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.False(pageModel.ModelState.IsValid);
        }

    }
}
