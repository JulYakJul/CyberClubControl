using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CybontrolX_Tests.Pages
{
    public class CreateEmployeeModelTests
    {
        private AppDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private Employee GetValidEmployee()
        {
            return new Employee
            {
                Name = "Иван",
                Surname = "Иванов",
                Patronymic = "Иванович",
                PhoneNumber = "1234567890",
                UserName = "ivan.ivanov",
                PasswordHash = "hashedpassword"
            };
        }

        [Fact]
        public async Task OnGetAsync_LoadsEmployees()
        {
            // Arrange
            var context = GetInMemoryDbContext("GetEmployeesTest");
            context.Employees.Add(GetValidEmployee());
            context.SaveChanges();
            var model = new CreateEmployeeModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            Assert.Single(model.Employees);
            Assert.Equal("Иван", model.Employees[0].Name);
        }

        [Fact]
        public async Task OnPostAddEmployeeAsync_InvalidModel_ReturnsPageWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext("InvalidModelTest");
            var model = new CreateEmployeeModel(context);
            model.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await model.OnPostAddEmployeeAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Ошибка: Некорректные данные.", model.NotificationMessage);
        }

        [Fact]
        public async Task OnPostAddEmployeeAsync_ValidModel_AddsEmployeeAndReturnsPage()
        {
            // Arrange
            var context = GetInMemoryDbContext("ValidEmployeeTest");
            var model = new CreateEmployeeModel(context)
            {
                NewEmployee = GetValidEmployee()
            };

            // Act
            var result = await model.OnPostAddEmployeeAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Сотрудник успешно добавлен в список сотрудников.", model.NotificationMessage);
            Assert.Single(context.Employees);
            Assert.Equal("Иван", context.Employees.FirstAsync().Result.Name);
        }

        [Fact]
        public async Task OnPostAddEmployeeAsync_WithDutySchedule_AssociatesSchedule()
        {
            // Arrange
            var context = GetInMemoryDbContext("ScheduleAssignmentTest");

            var dutySchedule = new DutySchedule
            {
                Id = 1,
                DutyDate = DateTime.UtcNow,
                ShiftStart = new TimeSpan(8, 0, 0),
                ShiftEnd = new TimeSpan(16, 0, 0)
            };

            context.DutySchedules.Add(dutySchedule);
            context.SaveChanges();

            var employee = GetValidEmployee();
            employee.DutyScheduleId = 1;

            var model = new CreateEmployeeModel(context)
            {
                NewEmployee = employee
            };

            // Act
            var result = await model.OnPostAddEmployeeAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.Equal("Сотрудник успешно добавлен в список сотрудников.", model.NotificationMessage);
            var savedEmployee = await context.Employees.Include(e => e.DutySchedule).FirstAsync();
            Assert.Equal(1, savedEmployee.DutyScheduleId);
            Assert.NotNull(savedEmployee.DutySchedule);
        }
    }
}
