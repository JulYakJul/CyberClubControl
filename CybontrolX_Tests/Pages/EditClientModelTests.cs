using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Moq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX_Tests.Pages
{
    public class EditClientModelTests
    {
        private readonly AppDbContext _context;

        public EditClientModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public void OnGet_WithValidId_ReturnsPageResult()
        {
            // Arrange
            var client = new Client { Id = 1, Name = "Test Client", PhoneNumber = "1234567890" };
            _context.Clients.Add(client);
            _context.SaveChanges();

            var pageModel = new EditClientModel(_context);

            // Act
            var result = pageModel.OnGet(1);

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal(client.Name, pageModel.EditClient.Name);
            Assert.Equal(client.PhoneNumber, pageModel.EditClient.PhoneNumber);
        }

        [Fact]
        public void OnGet_WithInvalidId_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditClientModel(_context);

            // Act
            var result = pageModel.OnGet(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void OnPost_WithValidInput_UpdatesClientAndRedirects()
        {
            // Arrange
            var client = new Client { Id = 1, Name = "Test Client", PhoneNumber = "1234567890" };
            _context.Clients.Add(client);
            _context.SaveChanges();

            var pageModel = new EditClientModel(_context)
            {
                EditClient = new Client { Id = 1, Name = "Updated Client", PhoneNumber = "0987654321" }
            };

            // Act
            var result = pageModel.OnPost();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Clients", redirectResult.PageName);

            var updatedClient = _context.Clients.Find(1);
            Assert.Equal("Updated Client", updatedClient.Name);
            Assert.Equal("0987654321", updatedClient.PhoneNumber);
        }

        [Fact]
        public void OnPost_WithInvalidInput_ReturnsPageResult()
        {
            // Arrange
            var client = new Client { Id = 1, Name = "Test Client", PhoneNumber = "1234567890" };
            _context.Clients.Add(client);
            _context.SaveChanges();

            var pageModel = new EditClientModel(_context)
            {
                EditClient = new Client { Id = 1, Name = "", PhoneNumber = "" } // Invalid input
            };

            pageModel.ModelState.AddModelError("Name", "Name is required.");
            pageModel.ModelState.AddModelError("PhoneNumber", "PhoneNumber is required.");

            // Act
            var result = pageModel.OnPost();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.False(pageModel.ModelState.IsValid);
        }

        [Fact]
        public void OnPost_WithNonExistentClient_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditClientModel(_context)
            {
                EditClient = new Client { Id = 999, Name = "Non-existent Client", PhoneNumber = "0000000000" }
            };

            // Act
            var result = pageModel.OnPost();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
