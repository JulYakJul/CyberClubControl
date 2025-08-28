using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Pages;
using CybontrolX.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace CybontrolX_Tests.Pages
{
    public class LoginModelTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPasswordHasher<Employee>> _mockPasswordHasher;
        private readonly Mock<IEmailService> _mockEmailService;

        public LoginModelTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockPasswordHasher = new Mock<IPasswordHasher<Employee>>();
            _mockEmailService = new Mock<IEmailService>();
        }

        [Fact]
        public async Task OnPostAsync_ValidLogin_RedirectsToReturnUrl()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                UserName = "testuser",
                PasswordHash = "hashedpassword",
                EmailConfirmed = true,
                Role = Role.Admin,
                Name = "Test",
                Patronymic = "Patronymic",
                PhoneNumber = "1234567890",
                Surname = "User"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Mock dependencies
            var mockPasswordHasher = new Mock<IPasswordHasher<Employee>>();
            mockPasswordHasher.Setup(p => p.VerifyHashedPassword(employee, "hashedpassword", "validpassword"))
                .Returns(PasswordVerificationResult.Success);

            // Mock UserManager
            var store = new Mock<IUserStore<Employee>>();
            var userManager = new Mock<UserManager<Employee>>(
                store.Object,
                null, null, null, null, null, null, null, null);

            // Setup UserManager to return our test employee
            userManager.Setup(u => u.FindByNameAsync("testuser"))
                      .ReturnsAsync(employee);

            // Mock HttpContext and authentication services
            var authService = new Mock<IAuthenticationService>();
            authService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices)
                      .Returns(serviceProvider.Object);

            // Mock SignInManager
            var mockSignInManager = new Mock<SignInManager<Employee>>(
                userManager.Object,
                new HttpContextAccessor { HttpContext = httpContext.Object },
                Mock.Of<IUserClaimsPrincipalFactory<Employee>>(),
                null, null, null, null);

            var mockEmailService = new Mock<IEmailService>();

            // Create model with all required dependencies
            var model = new LoginModel(
                _context,
                mockPasswordHasher.Object,
                mockEmailService.Object)
            {
                UserName = "testuser",
                Password = "validpassword",
                ReturnUrl = "/Index",
                PageContext = new PageContext
                {
                    HttpContext = httpContext.Object
                }
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Index", redirectResult.Url);
        }

        [Fact]
        public async Task OnPostAsync_InvalidLogin_ShowsErrorMessage()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                UserName = "testuser",
                PasswordHash = "hashedpassword",
                EmailConfirmed = true,
                Role = Enum.Parse<Role>("Admin"),
                Name = "Test",
                Patronymic = "Patronymic",
                PhoneNumber = "1234567890",
                Surname = "User"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(employee, "hashedpassword", "invalidpassword"))
                .Returns(PasswordVerificationResult.Failed);

            var model = new LoginModel(_context, _mockPasswordHasher.Object, _mockEmailService.Object)
            {
                UserName = "testuser",
                Password = "invalidpassword"
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(""));
            Assert.Contains("Неверный логин или пароль", model.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task OnPostAsync_UnconfirmedEmail_SendsConfirmationEmail()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                UserName = "testuser",
                PasswordHash = "hashedpassword",
                EmailConfirmed = false, // Ожидаем, что email не подтвержден
                Role = Enum.Parse<Role>("Admin"),
                Name = "Test",
                Patronymic = "Patronymic",
                PhoneNumber = "1234567890",
                Surname = "User"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(employee, "hashedpassword", "validpassword"))
                .Returns(PasswordVerificationResult.Success);

            // Мокаем SignInManager, передаем null в качестве дополнительного параметра Claims
            var mockSignInManager = new Mock<SignInManager<Employee>>();
            mockSignInManager.Setup(s => s.SignInAsync(It.IsAny<Employee>(), false, null))
                             .Returns(Task.CompletedTask);

            // Используем правильный конструктор для LoginModel
            var model = new LoginModel(_context, _mockPasswordHasher.Object, _mockEmailService.Object)
            {
                UserName = "testuser",
                Password = "validpassword"
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/ConfirmEmail", redirectResult.PageName);
            _mockEmailService.Verify(m => m.SendConfirmationEmail("testuser", It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task OnPostLogoutAsync_LogsOutUser_RedirectsToLoginPage()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                UserName = "testuser",
                PasswordHash = "hashedpassword",
                EmailConfirmed = true,
                Role = Enum.Parse<Role>("Admin"),
                Name = "Test",
                Patronymic = "Patronymic",
                PhoneNumber = "1234567890",
                Surname = "User"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>())).Returns(Task.CompletedTask);

            var serviceProvider = new ServiceCollection()
                .AddSingleton(mockAuthService.Object)
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var model = new LoginModel(_context, _mockPasswordHasher.Object, _mockEmailService.Object)
            {
                PageContext = new PageContext
                {
                    HttpContext = httpContext
                }
            };

            // Act
            var result = await model.OnPostLogoutAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Login", redirectResult.PageName);
        }

    }
}
