using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CybontrolX.Pages;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.AspNetCore.Http;

namespace CybontrolX_Tests.Pages
{
    public class ClientsModelTests
    {
        private readonly AppDbContext _context;

        public ClientsModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Очистка
            if (_context.Clients.Any())
            {
                _context.Clients.RemoveRange(_context.Clients);
                _context.SaveChanges();
            }

            // Добавление новых данных
            var clients = new List<Client>
            {
                new Client { Id = 1, Name = "Иван Иванов", PhoneNumber = "+79001234567" },
                new Client { Id = 2, Name = "Петр Петров", PhoneNumber = "+79007654321" },
                new Client { Id = 3, Name = "Александр Смирнов", PhoneNumber = "+79001122334" }
            };

            _context.Clients.AddRange(clients);
            _context.SaveChanges();

            Assert.True(_context.Clients.Count() == 3, "Данные клиентов не были добавлены в БД");
        }

        [Fact]
        public async Task OnGetAsync_WithoutFilter_ReturnsAllClientsSortedByName()
        {
            // Arrange
            var pageModel = new ClientsModel(_context);

            // Act
            await pageModel.OnGetAsync();

            // Assert
            Assert.Equal(3, pageModel.Clients.Count);
            Assert.Equal("Александр Смирнов", pageModel.Clients[0].Name); // По алфавиту
        }

        [Fact]
        public async Task OnGetAsync_WithSearchQuery_FilterClientsByName()
        {
            // Arrange
            var pageModel = new ClientsModel(_context)
            {
                SearchQuery = "Иван"
            };

            // Act
            await pageModel.OnGetAsync();

            // Assert
            Assert.Single(pageModel.Clients);
            Assert.Contains(pageModel.Clients, c => c.Name.Contains("Иван", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task OnGetAsync_WithSortDescending_SortsByPhoneDescending()
        {
            // Arrange
            var pageModel = new ClientsModel(_context)
            {
                SortColumn = "PhoneNumber",
                SortDescending = true
            };

            // Act
            await pageModel.OnGetAsync();

            // Assert
            var clients = pageModel.Clients;
            Assert.True(clients[0].PhoneNumber.CompareTo(clients[1].PhoneNumber) > 0);
        }

        [Fact]
        public async Task OnPostDeleteClientsAsync_WithValidIds_RemovesSelectedClients()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var requestForm = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["SelectedClientIds"] = "1,2"
            });

            var pageModel = new ClientsModel(_context)
            {
                PageContext = new PageContext { HttpContext = httpContext }
            };

            pageModel.Request.Form = requestForm;

            // Act
            var result = await pageModel.OnPostDeleteClientsAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Empty(_context.Clients.Where(c => c.Id == 1 || c.Id == 2));
            Assert.Single(_context.Clients);
        }

        [Fact]
        public async Task OnGetAllClients_ReturnsJsonListOfClients()
        {
            // Arrange
            var pageModel = new ClientsModel(_context);

            // Act
            var result = await pageModel.OnGetAllClients();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var clients = Assert.IsType<List<Client>>(jsonResult.Value);

            Assert.Equal(3, clients.Count);
            Assert.Contains(clients, c => c.Name == "Иван Иванов");
        }
    }
}