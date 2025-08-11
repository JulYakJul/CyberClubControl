using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CybontrolX_Tests.Pages
{
    public class EditTariffModelTests
    {
        private readonly AppDbContext _context;

        public EditTariffModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_TariffExists_ReturnsPageResult()
        {
            // Arrange
            var tariff = new Tariff { Id = 1, Name = "Basic Tariff", Price = 100, SessionTime = TimeSpan.FromHours(1), Days = new List<DayOfWeek> { DayOfWeek.Monday } };
            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            var pageModel = new EditTariffModel(_context);

            // Act
            var result = await pageModel.OnGetAsync(1);

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Basic Tariff", pageModel.Tariff.Name);
            Assert.Equal(1, pageModel.SessionTime);
            Assert.Contains("Monday", pageModel.SelectedDays);
        }

        [Fact]
        public async Task OnGetAsync_TariffNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditTariffModel(_context);

            // Act
            var result = await pageModel.OnGetAsync(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_ValidInput_UpdatesTariffAndRedirects()
        {
            // Arrange
            var tariff = new Tariff { Id = 1, Name = "Basic Tariff", Price = 100, SessionTime = TimeSpan.FromHours(1), Days = new List<DayOfWeek> { DayOfWeek.Monday } };
            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            var pageModel = new EditTariffModel(_context)
            {
                Tariff = new Tariff { Id = 1, Name = "Updated Tariff", Price = 150, SessionTime = TimeSpan.FromHours(2), Days = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday } },
                SessionTime = 2,
                SelectedDays = new List<string> { "Tuesday", "Wednesday" }
            };

            // Act
            var result = await pageModel.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Tariffs", redirectResult.PageName);

            var updatedTariff = await _context.Tariffs.FindAsync(1);
            Assert.Equal("Updated Tariff", updatedTariff.Name);
            Assert.Equal(150, updatedTariff.Price);
            Assert.Contains(DayOfWeek.Tuesday, updatedTariff.Days);
            Assert.Contains(DayOfWeek.Wednesday, updatedTariff.Days);
        }

        [Fact]
        public async Task OnPostAsync_TariffNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditTariffModel(_context)
            {
                Tariff = new Tariff { Id = 999, Name = "Nonexistent Tariff", Price = 100, SessionTime = TimeSpan.FromHours(1) },
                SessionTime = 1,
                SelectedDays = new List<string> { "Monday" }
            };

            // Act
            var result = await pageModel.OnPostAsync();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
