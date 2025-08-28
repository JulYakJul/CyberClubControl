using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Http;
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
    public class ClubStaffModelTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            return context;
        }

        private void SeedTestData(AppDbContext context)
        {
            context.Employees.AddRange(
                new Employee
                {
                    Id = 1,
                    Name = "Иван",
                    Surname = "Иванов",
                    Patronymic = "Иванович",
                    PhoneNumber = "111",
                    Role = Role.Admin,
                    UserName = "ivanov",
                    PasswordHash = "123"
                },
                new Employee
                {
                    Id = 2,
                    Name = "Петр",
                    Surname = "Петров",
                    Patronymic = "Петрович",
                    PhoneNumber = "222",
                    Role = Role.Manager,
                    UserName = "petrov",
                    PasswordHash = "456"
                }
            );

            context.DutySchedules.Add(new DutySchedule
            {
                Id = 1,
                EmployeeId = 1,
                DutyDate = DateTime.UtcNow.Date
            });

            context.SaveChanges();
        }


        [Fact]
        public async Task OnGetAsync_WithSearchQuery_FiltersBySurname()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            SeedTestData(context);

            var pageModel = new ClubStaffModel(context)
            {
                SearchQuery = "Петров"
            };

            // Act
            await pageModel.OnGetAsync();

            // Assert
            Assert.Single(pageModel.Employees);
            Assert.Equal("Петров", pageModel.Employees[0].Surname);
        }

        [Fact]
        public async Task OnPostDeleteEmployeesAsync_RemovesEmployees()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            SeedTestData(context);

            var pageModel = new ClubStaffModel(context);

            // Фальшивый Request.Form
            var formCollection = new Microsoft.AspNetCore.Http.FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "SelectedEmployeeIds", "1" }
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = formCollection;
            pageModel.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await pageModel.OnPostDeleteEmployeesAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            Assert.Single(context.Employees); // один удален, один остался
            Assert.DoesNotContain(context.Employees, e => e.Id == 1);
        }

        [Fact]
        public async Task OnGetAllEmployees_ReturnsJsonResultWithFormattedData()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            SeedTestData(context);

            var pageModel = new ClubStaffModel(context);

            // Act
            var result = await pageModel.OnGetAllEmployees();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            Assert.Equal(2, data.Count());
        }
    }
}
