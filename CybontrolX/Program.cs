using CybontrolX.DataBase;
using CybontrolX.DBModels;
using CybontrolX.Hubs;
using CybontrolX.Interfaces;
using CybontrolX.Models;
using CybontrolX.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClients", builder =>
    {
        builder.WithOrigins(
                "https://CybolX.ru:8443", // Сервер
                "http://localhost",        // Для тестирования
                "http://192.168.1.0/24"    // Вся локальная сеть
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<INetworkService, NetworkService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<SessionCleanupService>();
builder.WebHost.UseUrls("http://*:5000");

builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<UnlockHub>("/unlockhub");

app.Use(async (context, next) =>
{
    //if (!context.User.Identity.IsAuthenticated)
    //{
    //    Console.WriteLine($"Пользователь не аутентифицирован. Path: {context.Request.Path}");

    //    var allowedPaths = new[]
    //    {
    //        "/Login",
    //        "/Register",
    //        "/ConfirmEmail",
    //        "/ResendCode",
    //        "/api/yookassa/notify"
    //    };

    //    if (!allowedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
    //    {
    //        context.Response.Redirect("/Login");
    //        return;
    //    }
    //}

    await next();
});

app.MapRazorPages();

app.MapControllers();

app.MapFallbackToPage("/Clients");

app.Run();