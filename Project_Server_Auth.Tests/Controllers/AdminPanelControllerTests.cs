

// namespace Project_Server_Auth.Tests.Controllers

// AdminPanelControllerTests.cs
using DAL;
using DAL.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Project_Server_Auth.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Project_Server_Auth.Tests;

public class AdminPanelControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminPanelControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Удаляем реальную БД
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Добавляем InMemory БД
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });

            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_Should_Return401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin-panel/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatistics_WithAdminAuth_Should_ReturnStats()
    {
        // Arrange
        var adminToken = await CreateAdminAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin-panel/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<SimpleStatsDto>>(content, GetJsonOptions());

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalUsers.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateUser_ValidData_Should_CreateSuccessfully()
    {
        // Arrange
        var adminToken = await CreateAdminAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var newUser = new SimpleAdminCreateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Department = "IT",
            Password = "TestPass123",
            IsActive = true
        };

        var json = JsonSerializer.Serialize(newUser, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin-panel/users", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, GetJsonOptions());

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Be("Пользователь успешно создан");
    }

    [Fact]
    public async Task CreateUser_InvalidData_Should_Return400()
    {
        // Arrange
        var adminToken = await CreateAdminAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var invalidUser = new SimpleAdminCreateUserDto
        {
            FirstName = "", // Пустое имя
            LastName = "",
            Email = "invalid-email", // Неверный email
            Password = "123" // Короткий пароль
        };

        var json = JsonSerializer.Serialize(invalidUser, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin-panel/users", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_Should_ReturnPaginatedList()
    {
        // Arrange
        var adminToken = await CreateAdminAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin-panel/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<PagedResponseDto<SimpleAdminUserDto>>>(content, GetJsonOptions());

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Data.Should().NotBeNull();
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetNonExistentUser_Should_Return404()
    {
        // Arrange
        var adminToken = await CreateAdminAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin-panel/users/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, GetJsonOptions());

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Be("Пользователь не найден");
    }

    // Вспомогательные методы
    private async Task<string> CreateAdminAndGetTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Создаем роль Admin
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Создаем админа
        var adminEmail = "admin@test.com";
        var adminPassword = "AdminPassword123";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Test",
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Получаем JWT токен
        var loginDto = new LoginDto
        {
            Email = adminEmail,
            Password = adminPassword
        };

        var json = JsonSerializer.Serialize(loginDto, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<ApiResponse<AuthResponseDto>>(responseContent, GetJsonOptions());

        return authResult?.Data?.AccessToken ?? throw new InvalidOperationException("Не удалось получить токен админа");
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    // Вспомогательные классы
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
