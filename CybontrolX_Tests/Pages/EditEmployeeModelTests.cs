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

namespace CybontrolX_Tests.Pages
{
    public class EditEmployeeModelTests
    {
        private readonly AppDbContext _context;

        public EditEmployeeModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task OnGetAsync_EmployeeExists_ReturnsPageResult()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Иван",
                Surname = "Иванов",
                Patronymic = "Иванович",
                PhoneNumber = "1234567890",
                UserName = "ivanov",
                PasswordHash = "hashedpassword"
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var pageModel = new EditEmployeeModel(_context);

            // Act
            var result = await pageModel.OnGetAsync(1);

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.NotNull(pageModel.Employee);
            Assert.Equal("Иван", pageModel.Employee.Name);
        }

        [Fact]
        public async Task OnGetAsync_EmployeeNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditEmployeeModel(_context);

            // Act
            var result = await pageModel.OnGetAsync(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnPostEditEmployeeAsync_ValidEmployee_UpdatesEmployeeAndRedirects()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Иван",
                Surname = "Иванов",
                Patronymic = "Иванович",
                PhoneNumber = "1234567890",
                UserName = "ivanov",
                PasswordHash = "hashedpassword"
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var pageModel = new EditEmployeeModel(_context)
            {
                Employee = new Employee
                {
                    Id = 1,
                    Name = "Алексей",
                    Surname = "Алексеев",
                    Patronymic = "Алексеевич",
                    PhoneNumber = "9876543210",
                    UserName = "alekseev",
                    PasswordHash = "newhashedpassword"
                }
            };

            // Act
            var result = await pageModel.OnPostEditEmployeeAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("ClubStaff", redirectResult.PageName);

            var updatedEmployee = await _context.Employees.FindAsync(1);
            Assert.Equal("Алексей", updatedEmployee.Name);
            Assert.Equal("Алексеев", updatedEmployee.Surname);
            Assert.Equal("9876543210", updatedEmployee.PhoneNumber);
        }

        [Fact]
        public async Task OnPostEditEmployeeAsync_EmployeeNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var pageModel = new EditEmployeeModel(_context)
            {
                Employee = new Employee
                {
                    Id = 999,
                    Name = "Неизвестный",
                    Surname = "Неизвестный",
                    Patronymic = "Неизвестный",
                    PhoneNumber = "0000000000",
                    UserName = "unknown",
                    PasswordHash = "nohash"
                }
            };

            // Act
            var result = await pageModel.OnPostEditEmployeeAsync();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
        }
    }
}
