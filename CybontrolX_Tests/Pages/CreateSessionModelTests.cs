using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CybontrolX_Tests.Pages
{
    public class CreateSessionModelTests
    {
        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        //[Fact]
        //public async Task OnPostCreateSession_WithCashPayment_CreatesSessionAndPayment()
        //{
        //    // Arrange
        //    var context = GetInMemoryContext();

        //    var daysAsString = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday";
        //    var daysList = daysAsString
        //        .Split(',')
        //        .Select(d => Enum.Parse<DayOfWeek>(d))
        //        .ToList();

        //    var client = new Client { Name = "Тестовый Клиент", PhoneNumber = "1234567890" };
        //    var employee = new Employee
        //    {
        //        Name = "Имя",
        //        Surname = "Сотрудник",
        //        Patronymic = "Отчество",
        //        PhoneNumber = "9876543210",
        //        UserName = "employee1",
        //        PasswordHash = "hash"
        //    };

        //    var computer = new Computer { ComputerIP = "192.168.1.100" };
        //    var tariff = new Tariff
        //    {
        //        Name = "Тариф 1",
        //        Price = 100,
        //        SessionTime = TimeSpan.FromHours(1),
        //        Days = daysList
        //    };

        //    context.Clients.Add(client);
        //    context.Employees.Add(employee);
        //    context.Computers.Add(computer);
        //    context.Tariffs.Add(tariff);
        //    await context.SaveChangesAsync();

        //    var pageModel = new CreateSessionModel(context)
        //    {
        //        NewClientFullName = null,
        //        NewClientPhoneNumber = null,
        //        ExistingClientPhoneNumber = client.PhoneNumber,
        //        Session = new Session
        //        {
        //            ComputerId = computer.Id,
        //            EmployeeId = employee.Id
        //        },
        //        SelectedTariffs = new[] { tariff.Id },
        //        TariffQuantities = new Dictionary<int, int> { { tariff.Id, 1 } },
        //        PaymentMethod = "Cash"
        //    };

        //    // Act
        //    var result = await pageModel.OnPostCreateSession();

        //    // Assert
        //    var redirect = Assert.IsType<RedirectToPageResult>(result);
        //    Assert.Equal("Sessions", redirect.PageName);

        //    var savedSession = context.Sessions.Include(s => s.SessionTariffs).FirstOrDefault();
        //    Assert.NotNull(savedSession);
        //    Assert.True(savedSession.IsActive);
        //    Assert.Equal(client.Id, savedSession.ClientId);
        //    Assert.Equal(computer.Id, savedSession.ComputerId);
        //    Assert.Equal(employee.Id, savedSession.EmployeeId);

        //    var payment = context.Payments.FirstOrDefault();
        //    Assert.NotNull(payment);
        //    Assert.Equal(100, payment.Amount);
        //    Assert.Equal("Cash", payment.PaymentMethod);
        //}

        //[Fact]
        //public async Task OnPostCreateSession_WithoutClient_ReturnsPageWithError()
        //{
        //    // Arrange
        //    var context = GetInMemoryContext();
        //    var employee = new Employee { Surname = "Сотрудник" };
        //    var computer = new Computer { ComputerIP = "192.168.1.101" };
        //    context.Employees.Add(employee);
        //    context.Computers.Add(computer);
        //    await context.SaveChangesAsync();

        //    var pageModel = new CreateSessionModel(context)
        //    {
        //        Session = new Session
        //        {
        //            ComputerId = computer.Id,
        //            EmployeeId = employee.Id
        //        },
        //        PaymentMethod = "Cash",
        //        SelectedTariffs = Array.Empty<int>()
        //    };

        //    // Act
        //    var result = await pageModel.OnPostCreateSession();

        //    // Assert
        //    var page = Assert.IsType<PageResult>(result);
        //    Assert.Equal("Выберите клиента и компьютер.", pageModel.TempData["NotificationMessage"]);
        //}

        //[Fact]
        //public async Task OnPostCreateSession_WithoutTariff_ReturnsPageWithError()
        //{
        //    // Arrange
        //    var context = GetInMemoryContext();

        //    var client = new Client { Name = "Клиент", PhoneNumber = "00001111" };
        //    var employee = new Employee { Surname = "Сотрудник" };
        //    var computer = new Computer { ComputerIP = "192.168.1.102" };
        //    context.Clients.Add(client);
        //    context.Employees.Add(employee);
        //    context.Computers.Add(computer);
        //    await context.SaveChangesAsync();

        //    var pageModel = new CreateSessionModel(context)
        //    {
        //        ExistingClientPhoneNumber = client.PhoneNumber,
        //        Session = new Session
        //        {
        //            ComputerId = computer.Id,
        //            EmployeeId = employee.Id
        //        },
        //        PaymentMethod = "Cash",
        //        SelectedTariffs = Array.Empty<int>()
        //    };

        //    // Act
        //    var result = await pageModel.OnPostCreateSession();

        //    // Assert
        //    var page = Assert.IsType<PageResult>(result);
        //    Assert.Equal("Выберите хотя бы один тариф.", pageModel.TempData["NotificationMessage"]);
        //}

        [Fact]
        public void OnGetSearchClients_ReturnsMatchingPhoneNumbers()
        {
            // Arrange
            var context = GetInMemoryContext();
            context.Clients.Add(new Client { Name = "Иван", PhoneNumber = "123456" });
            context.Clients.Add(new Client { Name = "Петр", PhoneNumber = "123789" });
            context.Clients.Add(new Client { Name = "Сидор", PhoneNumber = "987654" });
            context.SaveChanges();

            var pageModel = new CreateSessionModel(context);

            // Act
            var result = pageModel.OnGetSearchClients("123");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var list = Assert.IsType<List<string>>(jsonResult.Value);
            Assert.Equal(2, list.Count);
            Assert.Contains("123456", list);
            Assert.Contains("123789", list);
        }
    }
}
