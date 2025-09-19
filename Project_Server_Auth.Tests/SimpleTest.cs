// SimpleTest.cs
namespace Project_Server_Auth.Tests;

public class SimpleTest
{
    [Fact]
    public void Test_Should_Pass()
    {
        // Arrange
        bool expected = true;

        // Act
        bool actual = true;

        // Assert
        Assert.True(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Math_Should_Work()
    {
        // Act
        int result = 2 + 2;

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void String_Operations_Should_Work()
    {
        // Arrange
        string text = "Hello World";

        // Act
        bool contains = text.Contains("World");
        int length = text.Length;

        // Assert
        Assert.True(contains);
        Assert.Equal(11, length);
    }
}