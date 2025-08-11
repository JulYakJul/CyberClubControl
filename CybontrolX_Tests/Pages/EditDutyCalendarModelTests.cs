using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CybontrolX_Tests.Pages
{
    public class EditDutyCalendarModelTests
    {
        private readonly AppDbContext _context;

        public EditDutyCalendarModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_ValidEmployeeId_ReturnsDutyDates()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Анна",
                Surname = "Сидорова",
                PasswordHash = "hashedpassword",
                Patronymic = "Ивановна",
                PhoneNumber = "123456789",
                UserName = "anna_s"
            };
            _context.Employees.Add(employee);

            var dutySchedule = new DutySchedule
            {
                EmployeeId = 1,
                DutyDate = new DateTime(2025, 05, 10),
                ShiftStart = new TimeSpan(9, 0, 0),
                ShiftEnd = new TimeSpan(17, 0, 0)
            };
            _context.DutySchedules.Add(dutySchedule);
            await _context.SaveChangesAsync();

            // Create a mock HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            var model = new CreateEditDutyCalendarModel(_context)
            {
                PageContext = new PageContext
                {
                    HttpContext = httpContext
                }
            };

            // Act
            var result = await model.OnGetAsync(1, null);

            // Assert
            Assert.NotNull(result);
            var pageResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(pageResult.Value);
            var valueType = pageResult.Value.GetType();
            Assert.True(valueType.Name.Contains("AnonymousType"));
            var dutyDatesProperty = valueType.GetProperty("dutyDates");
            Assert.NotNull(dutyDatesProperty);
            var dutyDates = dutyDatesProperty.GetValue(pageResult.Value) as string;
            Assert.NotNull(dutyDates);
            Assert.Contains("2025-05-10", dutyDates);
        }

        [Fact]
        public async Task OnPostAsync_ValidData_CreatesSchedulesAndRedirects()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Анна",
                Surname = "Сидорова",
                PasswordHash = "hashedpassword",
                Patronymic = "Ивановна",
                PhoneNumber = "123456789",
                UserName = "anna_s"
            };
            _context.Employees.Add(employee);
            _context.SaveChanges();

            var model = new CreateEditDutyCalendarModel(_context)
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

            var schedules = _context.DutySchedules.Where(s => s.EmployeeId == 1).ToList();
            Assert.Equal(2, schedules.Count);
        }

        [Fact]
        public async Task OnPostAsync_EmployeeIdIsZero_ReturnsPageWithError()
        {
            // Arrange
            var model = new CreateEditDutyCalendarModel(_context)
            {
                EmployeeId = 0,
                DutyDates = "2025-05-10,2025-05-11",
                ShiftStart = new TimeSpan(9, 0, 0),
                ShiftEnd = new TimeSpan(17, 0, 0)
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey("EmployeeId"));
        }
    }
}
