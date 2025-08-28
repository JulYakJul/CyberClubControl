using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using CybontrolX.Services;
using CybontrolX.Interfaces;

namespace CybontrolX_Tests.Pages
{
    public class CreateComputerModelTests
    {
        private readonly AppDbContext _context;

        public CreateComputerModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        //[Fact]
        //public void OnPostAddComputer_WithValidIP_AddsComputerToDatabase()
        //{
        //    // Arrange
        //    var mockNetworkService = new Mock<INetworkService>();
        //    mockNetworkService.Setup(s => s.TryConnect(It.IsAny<string>())).Returns(true);

        //    // Теперь мы передаем оба параметра в конструктор
        //    var pageModel = new CreateComputerModel(_context, mockNetworkService.Object)
        //    {
        //        NewComputer = new Computer { ComputerIP = "192.168.1.1" }
        //    };

        //    // Act
        //    var result = pageModel.OnPostAddComputer();

        //    // Assert
        //    var pageResult = Assert.IsType<PageResult>(result);
        //    Assert.Single(_context.Computers); // Проверяем, что компьютер добавлен
        //    Assert.Contains("Компьютер успешно добавлен!", pageModel.NotificationMessage);
        //}

        //[Fact]
        //public void OnPostAddComputer_WithInvalidIP_DoesNotAddComputer()
        //{
        //    // Arrange
        //    var mockNetworkService = new Mock<INetworkService>();
        //    mockNetworkService.Setup(s => s.TryConnect(It.IsAny<string>())).Returns(false);

        //    var pageModel = new CreateComputerModel(_context, mockNetworkService.Object)
        //    {
        //        NewComputer = new Computer { ComputerIP = "invalid.ip.address" }
        //    };

        //    // Act
        //    var result = pageModel.OnPostAddComputer();

        //    // Assert
        //    var pageResult = Assert.IsType<PageResult>(result);
        //    Assert.Empty(_context.Computers); // Проверяем, что компьютер не добавлен
        //    Assert.Contains("Не удалось подключиться к компьютеру", pageModel.NotificationMessage);
        //}
    }
}