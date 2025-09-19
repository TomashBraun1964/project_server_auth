// Test_01_Authorization.cs
using DAL;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace Project_Server_Auth.Tests;

public class Test_01_Authorization : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public Test_01_Authorization(WebApplicationFactory<Program> factory)
    {
        var testFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        });

        _client = testFactory.CreateClient();
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
    public async Task GetUsers_WithoutAuth_Should_Return401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin-panel/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}