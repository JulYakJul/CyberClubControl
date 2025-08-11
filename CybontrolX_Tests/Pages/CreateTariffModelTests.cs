using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CybontrolX_Tests.Pages
{
    public class CreateTariffModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnPostAsync_ValidInput_CreatesTariffAndRedirects()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var model = new CreateTariffModel(context)
            {
                Name = "Test Tariff",
                SessionTime = 2.5,
                Days = new List<string> { "Monday", "Tuesday", "Wednesday" },
                Price = 150
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Tariffs", redirectResult.PageName);
            var savedTariff = context.Tariffs.FirstOrDefault();
            Assert.NotNull(savedTariff);
            Assert.Equal("Test Tariff", savedTariff.Name);
            Assert.Equal(TimeSpan.FromHours(2.5), savedTariff.SessionTime);
            Assert.Equal(3, savedTariff.Days.Count);
            Assert.Contains(DayOfWeek.Monday, savedTariff.Days);
            Assert.Equal(150, savedTariff.Price);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModelState_ReturnsPageResult()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var model = new CreateTariffModel(context);
            model.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Empty(context.Tariffs);
        }

        [Fact]
        public async Task OnPostAsync_UnknownDay_ThrowsArgumentException()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var model = new CreateTariffModel(context)
            {
                Name = "Test Tariff",
                SessionTime = 1,
                Days = new List<string> { "Funday" },
                Price = 100
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => model.OnPostAsync());
        }
    }
}
