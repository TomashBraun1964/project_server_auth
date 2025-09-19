//// Test_05_CreateUser.cs
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
//using System.Net;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;

//namespace Project_Server_Auth.Tests;

//public class Test_05_CreateUser : IClassFixture<WebApplicationFactory<Program>>
//{
//    private readonly WebApplicationFactory<Program> _factory;
//    private readonly HttpClient _client;

//    public Test_05_CreateUser(WebApplicationFactory<Program> factory)
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
//    public async Task CreateUser_ValidData_Should_CreateSuccessfully()
//    {
//        // Arrange
//        var adminToken = await CreateAdminAndGetTokenAsync();
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

//        var newUser = new SimpleAdminCreateUserDto
//        {
//            FirstName = "Test",
//            LastName = "User",
//            Email = "testuser@example.com",
//            Department = "IT",
//            Password = "TestPassword123A",
//            IsActive = true
//        };

//        var json = JsonSerializer.Serialize(newUser, GetJsonOptions());
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        // Act
//        var response = await _client.PostAsync("/api/admin-panel/users", content);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);

//        var responseContent = await response.Content.ReadAsStringAsync();
//        var result = JsonSerializer.Deserialize<CreateUserResponse>(responseContent, GetJsonOptions());

//        result.Should().NotBeNull();
//        result!.Success.Should().BeTrue();
//        result.Message.Should().Be("Пользователь успешно создан");
//    }

//    [Fact]
//    public async Task CreateUser_InvalidData_Should_Return400()
//    {
//        // Arrange
//        var adminToken = await CreateAdminAndGetTokenAsync();
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

//        var invalidUser = new SimpleAdminCreateUserDto
//        {
//            FirstName = "", // Пустое имя
//            LastName = "",  // Пустая фамилия
//            Email = "invalid-email", // Неверный email
//            Password = "123" // Короткий пароль
//        };

//        var json = JsonSerializer.Serialize(invalidUser, GetJsonOptions());
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        // Act
//        var response = await _client.PostAsync("/api/admin-panel/users", content);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
//    }

//    private async Task<string> CreateAdminAndGetTokenAsync()
//    {
//        using var scope = _factory.Services.CreateScope();
//        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//        if (!await roleManager.RoleExistsAsync("Admin"))
//        {
//            await roleManager.CreateAsync(new IdentityRole("Admin"));
//        }

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

//        var loginDto = new LoginDto { Email = adminEmail, Password = adminPassword };
//        var json = JsonSerializer.Serialize(loginDto, GetJsonOptions());
//        var content = new StringContent(json, Encoding.UTF8, "application/json");

//        var response = await _client.PostAsync("/api/auth/login", content);
//        var responseContent = await response.Content.ReadAsStringAsync();
//        var authResult = JsonSerializer.Deserialize<AuthResponse>(responseContent, GetJsonOptions());

//        return authResult?.Data?.AccessToken ?? throw new InvalidOperationException("Failed to get token");
//    }

//    private static JsonSerializerOptions GetJsonOptions() => new()
//    {
//        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//        PropertyNameCaseInsensitive = true
//    };

//    public class SimpleAdminCreateUserDto
//    {
//        public string FirstName { get; set; } = string.Empty;
//        public string LastName { get; set; } = string.Empty;
//        public string Email { get; set; } = string.Empty;
//        public string? Department { get; set; }
//        public string Password { get; set; } = string.Empty;
//        public bool IsActive { get; set; } = true;
//    }

//    public class CreateUserResponse
//    {
//        public bool Success { get; set; }
//        public string? Message { get; set; }
//    }

//    public class AuthResponse
//    {
//        public bool Success { get; set; }
//        public AuthResponseDto? Data { get; set; }
//    }
//}