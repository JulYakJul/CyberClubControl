using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CybontrolX_Tests.Pages
{
    public class DeleteDutyCalendarModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_LoadsEmployeesWithSchedules()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var employee = new Employee
            {
                Id = 1,
                Name = "Иван",
                Surname = "Иванов",
                Patronymic = "Иванович",
                PhoneNumber = "1234567890",
                UserName = "ivan",
                PasswordHash = "123"
            };
            context.Employees.Add(employee);
            context.DutySchedules.Add(new DutySchedule { EmployeeId = 1, ShiftStart = TimeSpan.FromHours(9), ShiftEnd = TimeSpan.FromHours(17), DutyDate = DateTime.Today });
            context.SaveChanges();

            var model = new DeleteDutyCalendarModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            Assert.Single(model.Employees);
            Assert.Equal(1, model.Employees[0].Id);
        }

        [Fact]
        public async Task OnPostAsync_WithValidInput_DeletesSchedulesAndRedirects()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var employee = new Employee
            {
                Id = 1,
                Name = "Анна",
                Surname = "Сидорова",
                Patronymic = "Петровна",
                PhoneNumber = "9876543210",
                UserName = "anna",
                PasswordHash = "123"
            };
            context.Employees.Add(employee);
            context.DutySchedules.AddRange(
                new DutySchedule
                {
                    EmployeeId = 1,
                    DutyDate = DateTime.Today,
                    ShiftStart = TimeSpan.FromHours(9),
                    ShiftEnd = TimeSpan.FromHours(17)
                },
                new DutySchedule
                {
                    EmployeeId = 1,
                    DutyDate = DateTime.Today.AddDays(1),
                    ShiftStart = TimeSpan.FromHours(9),
                    ShiftEnd = TimeSpan.FromHours(17)
                });
            context.SaveChanges();

            var model = new DeleteDutyCalendarModel(context)
            {
                EmployeeId = 1
            };

            // Устанавливаем PageContext и ViewData
            var modelState = new ModelStateDictionary();
            var metadataProvider = new EmptyModelMetadataProvider();

            model.PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(metadataProvider, modelState)
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            Assert.Empty(context.DutySchedules.ToList());
        }

        [Fact]
        public async Task OnPostAsync_WithMissingEmployeeId_ReturnsPageWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var model = new DeleteDutyCalendarModel(context)
            {
                EmployeeId = 0
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("EmployeeId"));
        }
    }
}
