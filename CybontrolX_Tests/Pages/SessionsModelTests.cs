using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using CybontrolX.DBModels;
using CybontrolX.DataBase;
using CybontrolX.Pages;
using Newtonsoft.Json;

namespace CybontrolX_Tests.Pages
{
    public class SessionsModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_FiltersAndSortsSessions()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            var client1 = new Client
            {
                Id = 1,
                Name = "Алексей",
                PhoneNumber = "1234567890"
            };
            var client2 = new Client
            {
                Id = 2,
                Name = "Борис",
                PhoneNumber = "0987654321"
            };

            var computer = new Computer
            {
                Id = 1,
                ComputerIP = "192.168.0.1",
                Status = true
            };

            context.Clients.AddRange(client1, client2);
            context.Computers.Add(computer);
            context.Sessions.AddRange(
                new Session
                {
                    Id = 1,
                    ClientId = 1,
                    ComputerId = 1,
                    SessionStartTime = DateTime.UtcNow.AddHours(-2),
                    IsActive = true
                },
                new Session
                {
                    Id = 2,
                    ClientId = 2,
                    ComputerId = 1,
                    SessionStartTime = DateTime.UtcNow.AddHours(-1),
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();

            var model = new SessionsModel(context)
            {
                SearchQuery = "Алексей",
                SortColumn = "Name",
                SortDescending = false
            };

            // Act
            await model.OnGetAsync();

            // Assert
            Assert.Single(model.Sessions);
            Assert.Equal("Алексей", model.Sessions.First().Client.Name);
        }

        [Fact]
        public async Task OnGetAllSessions_ReturnsJsonResult()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            context.Clients.Add(new Client
            {
                Id = 1,
                Name = "Клиент1",
                PhoneNumber = "1234567890"
            });

            context.Computers.Add(new Computer
            {
                Id = 1,
                ComputerIP = "10.0.0.1",
                Status = true
            });

            context.Sessions.Add(new Session
            {
                Id = 1,
                ClientId = 1,
                ComputerId = 1,
                SessionStartTime = DateTime.UtcNow,
                IsActive = true
            });

            await context.SaveChangesAsync();

            var model = new SessionsModel(context);

            // Act
            var result = await model.OnGetAllSessions();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);

            // Option 1: Convert to dictionary if using anonymous types
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                JsonConvert.SerializeObject(jsonResult.Value));

            var firstSession = data.First();
            Assert.Equal("Клиент1", firstSession["clientName"].ToString());
            Assert.Equal("10.0.0.1", firstSession["computerIP"].ToString());
            Assert.True((bool)firstSession["isActive"]);
        }

        [Fact]
        public async Task OnPostDeleteSessions_ClosesActiveSessions()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            context.Clients.Add(new Client
            {
                Id = 1,
                Name = "Test",
                PhoneNumber = "1234567890"
            });

            context.Computers.Add(new Computer
            {
                Id = 1,
                ComputerIP = "192.168.34.11"
            });

            context.Sessions.Add(new Session
            {
                Id = 1,
                ClientId = 1,
                ComputerId = 1,
                SessionStartTime = DateTime.UtcNow.AddHours(-1),
                IsActive = true
            });

            await context.SaveChangesAsync();

            var model = new SessionsModel(context);

            // Act
            var result = await model.OnPostDeleteSessions("1");

            // Assert
            var updatedSession = await context.Sessions.FirstAsync();
            Assert.False(updatedSession.IsActive);
            Assert.NotNull(updatedSession.SessionEndTime);
            Assert.IsType<RedirectToPageResult>(result);
        }

        [Fact]
        public async Task OnPostDeleteSessions_DoesNothingIfEmpty()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var model = new SessionsModel(context);

            // Act
            var result = await model.OnPostDeleteSessions("");

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
        }
    }
}
