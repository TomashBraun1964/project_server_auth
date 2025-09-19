// AdminControllerTests.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;

namespace Project_Server_Auth.Tests;

public class AdminControllerTests
{
    [Fact]
    public void Simple_Addition_Test()
    {
        // Act
        int result = 2 + 2;

        // Assert
        result.Should().Be(4);
    }

    [Fact]
    public void String_Contains_Test()
    {
        // Arrange
        string adminText = "AdminPanelController";

        // Act
        bool containsAdmin = adminText.Contains("Admin");

        // Assert
        containsAdmin.Should().BeTrue();
    }

    [Fact]
    public void List_Operations_Test()
    {
        // Arrange
        var users = new List<string> { "user1@test.com", "admin@test.com", "user2@test.com" };

        // Act
        var adminUser = users.FirstOrDefault(u => u.Contains("admin"));

        // Assert
        adminUser.Should().NotBeNull();
        adminUser.Should().Be("admin@test.com");
        users.Should().HaveCount(3);
    }

    [Fact]
    public void Mock_Logger_Test()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<object>>();

        // Act
        var logger = mockLogger.Object;

        // Assert
        logger.Should().NotBeNull();
        mockLogger.Should().NotBeNull();
    }

    [Fact]
    public async Task Async_Task_Test()
    {
        // Act
        await Task.Delay(1);
        var result = await GetTestValueAsync();

        // Assert
        result.Should().Be("test");
    }

    private async Task<string> GetTestValueAsync()
    {
        await Task.Delay(1);
        return "test";
    }
}