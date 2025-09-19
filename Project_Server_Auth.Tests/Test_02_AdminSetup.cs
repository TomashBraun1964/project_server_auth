//// Test_02_AdminSetup.cs
//using DAL;
//using DAL.Models;
//using FluentAssertions;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.VisualStudio.TestPlatform.TestHost;
//using Project_Server_Auth.Dtos;
//using System.Text;
//using System.Text.Json;

//namespace Project_Server_Auth.Tests;

//public class Test_02_AdminSetup : IClassFixture<WebApplicationFactory<Program>>
//{
//    private readonly WebApplicationFactory<Program> _factory;
//    private readonly HttpClient _client;

//    public Test_02_AdminSetup(WebApplicationFactory<Program> factory)
//    {
//        _factory = factory.WithWebHostBuilder(builder =>
//        {
//            builder.ConfigureServices(services =>
//            {
//                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
//                if (descriptor != null) services.Remove(descriptor);
//                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
//            });
//        });
//        _client = _factory.CreateClient();
//    }

//    [Fact]
//    public async Task CreateAdmin_Should_CreateSuccessfully()
//    {
//        // Arrange & Act
//        var token = await CreateAdminAndGetTokenAsync();

//        // Assert
//        token.Should().NotBeNullOrEmpty();
//    }

//    private async Task<string> CreateAdminAndGetTokenAsync()
//    {
//        using var scope = _factory.Services.CreateScope();
//        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//        // Создаем роль Admin
//        if (!await roleManager.RoleExistsAsync("Admin"))
//        {
//            await roleManager.CreateAsync(new IdentityRole("Admin"));
//        }

//        // Создаем админа
//        const string adminEmail = "admin@test.com";
//        const string adminPassword = "AdminPassword123A";

//        var admin = new ApplicationUser
//        {
//            UserName = adminEmail,
//            Email = adminEmail,
//            FirstName = "Admin",
//            LastName = "Test",
//            IsActive = true,
//            EmailConfirmed = true
//        };

//        var createResult = await userManager.CreateAsync(admin, adminPassword);
//        if (createResult.Succeeded)
//        {
//            await userManager.AddToRoleAsync(admin, "Admin");
//        }

//        // Получаем токен
//        var loginDto = new LoginDto { Email = adminEmail, Password = adminPassword };
//        var json = JsonSerializer.Serialize(loginDto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        var response = await _client.PostAsync("/api/auth/login", content);
//        var responseContent = await response.Content.ReadAsStringAsync();
//        var authResult = JsonSerializer.Deserialize<TestAuthResponse>(responseContent,
//            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });

//        return authResult?.Data?.AccessToken ?? throw new InvalidOperationException("Не удалось получить токен");
//    }

//    public class TestAuthResponse
//    {
//        public bool Success { get; set; }
//        public AuthResponseDto? Data { get; set; }
//    }
//}