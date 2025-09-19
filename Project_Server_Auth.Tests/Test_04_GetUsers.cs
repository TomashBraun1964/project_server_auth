//// Test_04_GetUsers.cs
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

//public class Test_04_GetUsers : IClassFixture<WebApplicationFactory<Program>>
//{
//    private readonly WebApplicationFactory<Program> _factory;
//    private readonly HttpClient _client;

//    public Test_04_GetUsers(WebApplicationFactory<Program> factory)
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
//    public async Task GetUsers_WithAdminAuth_Should_ReturnPaginatedList()
//    {
//        // Arrange
//        var adminToken = await CreateAdminAndGetTokenAsync();
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

//        // Act
//        var response = await _client.GetAsync("/api/admin-panel/users?pageNumber=1&pageSize=10");

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);

//        var content = await response.Content.ReadAsStringAsync();
//        var result = JsonSerializer.Deserialize<UsersResponse>(content, GetJsonOptions());

//        result.Should().NotBeNull();
//        result!.Success.Should().BeTrue();
//        result.Data.Should().NotBeNull();
//        result.Data!.PageNumber.Should().Be(1);
//        result.Data.PageSize.Should().Be(10);
//        result.Data.TotalCount.Should().BeGreaterThan(0);
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

//    public class UsersResponse
//    {
//        public bool Success { get; set; }
//        public PagedData? Data { get; set; }
//    }

//    public class PagedData
//    {
//        public int PageNumber { get; set; }
//        public int PageSize { get; set; }
//        public int TotalCount { get; set; }
//        public List<object> Data { get; set; } = new();
//    }

//    public class AuthResponse
//    {
//        public bool Success { get; set; }
//        public AuthResponseDto? Data { get; set; }
//    }
//}