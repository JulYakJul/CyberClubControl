using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CybontrolX_Tests.Pages
{
    public class TariffsModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnGet_WithSearchTerm_ReturnsFilteredTariffs()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Tariffs.AddRange(
                new Tariff { Name = "Игровой" },
                new Tariff { Name = "Обычный" },
                new Tariff { Name = "Премиум" }
            );
            context.SaveChanges();

            var pageModel = new TariffsModel(context);

            // Act
            await pageModel.OnGet("игров");

            // Assert
            Assert.Single(pageModel.Tariffs);
            Assert.Equal("Игровой", pageModel.Tariffs[0].Name);
        }

        [Fact]
        public async Task OnGet_WithoutSearchTerm_ReturnsAllTariffs()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Tariffs.AddRange(
                new Tariff { Name = "Игровой" },
                new Tariff { Name = "Обычный" }
            );
            context.SaveChanges();

            var pageModel = new TariffsModel(context);

            // Act
            await pageModel.OnGet(null);

            // Assert
            Assert.Equal(2, pageModel.Tariffs.Count);
        }

        [Fact]
        public void OnGetSearch_WithSearchTerm_ReturnsPartialWithFilteredTariffs()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Tariffs.AddRange(
                new Tariff { Name = "Игровой" },
                new Tariff { Name = "Премиум" }
            );
            context.SaveChanges();

            // Create necessary services for partial view rendering
            var services = new ServiceCollection();
            services.AddSingleton<IModelMetadataProvider>(new EmptyModelMetadataProvider());
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var pageModel = new TariffsModel(context)
            {
                PageContext = new PageContext
                {
                    HttpContext = httpContext,
                    ViewData = new ViewDataDictionary(
                        serviceProvider.GetRequiredService<IModelMetadataProvider>(),
                        new ModelStateDictionary())
                }
            };

            // Act
            var result = pageModel.OnGetSearch("Прем");

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TariffsPartial", partialResult.ViewName);

            var model = Assert.IsAssignableFrom<List<Tariff>>(partialResult.Model);
            Assert.Single(model);
            Assert.Equal("Премиум", model[0].Name);
        }

        [Fact]
        public async Task OnGetSearch_WithoutSearchTerm_ReturnsAllTariffs()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Tariffs.AddRange(
                new Tariff { Id = 1, Name = "Игровой" },
                new Tariff { Id = 2, Name = "Премиум" }
            );
            await context.SaveChangesAsync();

            // Create necessary services for partial view rendering
            var services = new ServiceCollection();
            services.AddSingleton<IModelMetadataProvider>(new EmptyModelMetadataProvider());
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var model = new TariffsModel(context)
            {
                PageContext = new PageContext
                {
                    HttpContext = httpContext,
                    ViewData = new ViewDataDictionary(
                        serviceProvider.GetRequiredService<IModelMetadataProvider>(),
                        new ModelStateDictionary())
                }
            };

            // Act
            var result = model.OnGetSearch(null);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TariffsPartial", partialResult.ViewName);

            var modelData = Assert.IsAssignableFrom<List<Tariff>>(partialResult.Model);
            Assert.Equal(2, modelData.Count);
        }

        [Fact]
        public async Task OnPostDeleteAsync_RemovesTariffAndRedirects()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var tariff = new Tariff { Name = "Удаляемый" };
            context.Tariffs.Add(tariff);
            context.SaveChanges();

            var pageModel = new TariffsModel(context);

            // Act
            var result = await pageModel.OnPostDeleteAsync(tariff.Id);

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            Assert.Empty(context.Tariffs.ToList());
        }

        [Theory]
        [InlineData(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday }, "Пн-Ср")]
        [InlineData(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }, "Пн, Ср, Пт")]
        [InlineData(new DayOfWeek[] { }, "")]
        public void FormatDays_ReturnsExpectedString(DayOfWeek[] days, string expected)
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var pageModel = new TariffsModel(context);

            // Act
            var result = pageModel.FormatDays(days.ToList());

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
