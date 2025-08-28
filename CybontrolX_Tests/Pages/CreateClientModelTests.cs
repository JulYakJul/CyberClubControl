using CybontrolX.DataBase;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DBModels;

namespace CybontrolX_Tests.Pages
{
    public class CreateClientModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnPostAddClientAsync_ValidClient_RedirectsToClientsPage()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var pageModel = new CreateClientModel(context)
            {
                NewClient = new Client
                {
                    Name = "Алексей Смирнов",
                    PhoneNumber = "+79991234567"
                }
            };

            // Act
            var result = await pageModel.OnPostAddClientAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Clients", redirectResult.PageName);
            Assert.Equal(1, context.Clients.Count());
            Assert.Equal("Алексей Смирнов", context.Clients.First().Name);
        }

        [Fact]
        public async Task OnPostAddClientAsync_InvalidModelState_ReturnsPageWithErrorMessage()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var pageModel = new CreateClientModel(context);
            pageModel.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await pageModel.OnPostAddClientAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Ошибка: Некорректные данные.", pageModel.NotificationMessage);
            Assert.Empty(context.Clients);
        }
    }

}
