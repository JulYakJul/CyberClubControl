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
    public class CreateEditDutyCalendarModelTests
    {
        private AppDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_LoadsEmployees()
        {
            // Arrange
            var context = GetInMemoryDbContext("GetEmployeesTest");

            context.Employees.Add(new Employee
            {
                Id = 1,
                Name = "Иван",
                Surname = "Иванов",
                Patronymic = "Иванович",
                PhoneNumber = "1234567890",
                UserName = "ivanov",
                PasswordHash = "hashedpassword"
            });

            context.Employees.Add(new Employee
            {
                Id = 2,
                Name = "Петр",
                Surname = "Петров",
                Patronymic = "Петрович",
                PhoneNumber = "0987654321",
                UserName = "petrov",
                PasswordHash = "hashedpassword"
            });

            context.SaveChanges();

            var model = new CreateEditDutyCalendarModel(context);

            // Act
            var result = await model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal(2, model.Employees.Count);
        }

        [Fact]
        public async Task OnPostAsync_WithInvalidEmployeeId_ReturnsPageWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext("InvalidEmployeeIdTest");
            var model = new CreateEditDutyCalendarModel(context)
            {
                EmployeeId = 0,
                DutyDates = "2025-05-01",
                ShiftStart = TimeSpan.FromHours(9),
                ShiftEnd = TimeSpan.FromHours(18)
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("EmployeeId"));
        }

        [Fact]
        public async Task OnPostAsync_ValidInput_CreatesSchedulesAndRedirects()
        {
            // Arrange
            var context = GetInMemoryDbContext("ValidPostTest");

            var employee = new Employee
            {
                Id = 1,
                Name = "Анна",
                Surname = "Сидорова",
                Patronymic = "Игоревна",
                PhoneNumber = "1234567890",
                UserName = "anna.sidorova",
                PasswordHash = "hashedpassword"
            };

            context.Employees.Add(employee);
            context.SaveChanges();

            var model = new CreateEditDutyCalendarModel(context)
            {
                EmployeeId = 1,
                DutyDates = "2025-05-10,2025-05-11",
                ShiftStart = new TimeSpan(9, 0, 0),
                ShiftEnd = new TimeSpan(17, 0, 0)
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/DutyCalendar", redirectResult.PageName);

            var schedules = context.DutySchedules.Where(s => s.EmployeeId == 1).ToList();
            Assert.Equal(2, schedules.Count);
        }

    }
}
