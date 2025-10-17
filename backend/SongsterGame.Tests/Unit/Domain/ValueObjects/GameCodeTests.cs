using SongsterGame.Api.Domain.ValueObjects;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Domain.ValueObjects;

public class GameCodeTests
{
    [Fact]
    [UnitTest]
    public void Create_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var validCode = "ABCD1234";

        // Act
        var result = GameCode.Create(validCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABCD1234", result.Value.Value);
    }

    [Fact]
    [UnitTest]
    public void Create_WithLowercaseCode_ConvertsToUppercase()
    {
        // Arrange
        var lowercaseCode = "abcd1234";

        // Act
        var result = GameCode.Create(lowercaseCode);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABCD1234", result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [UnitTest]
    public void Create_WithEmptyCode_ReturnsFailure(string invalidCode)
    {
        // Act
        var result = GameCode.Create(invalidCode);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHI")]
    [UnitTest]
    public void Create_WithInvalidLength_ReturnsFailure(string invalidCode)
    {
        // Act
        var result = GameCode.Create(invalidCode);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("exactly 8 characters", result.Error.Message);
    }

    [Theory]
    [InlineData("ABCD@123")]
    [InlineData("ABCD-123")]
    [InlineData("ABCD 123")]
    [UnitTest]
    public void Create_WithInvalidCharacters_ReturnsFailure(string invalidCode)
    {
        // Act
        var result = GameCode.Create(invalidCode);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("invalid characters", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public void Generate_ReturnsValid8CharacterCode()
    {
        // Act
        var gameCode = GameCode.Generate();

        // Assert
        Assert.Equal(8, gameCode.Value.Length);
        Assert.All(gameCode.Value, c => Assert.True(char.IsLetterOrDigit(c)));
        Assert.All(gameCode.Value, c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
    }

    [Fact]
    [UnitTest]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var gameCode = GameCode.Generate();

        // Act
        string codeString = gameCode;

        // Assert
        Assert.Equal(gameCode.Value, codeString);
    }
}