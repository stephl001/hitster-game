using SongsterGame.Api.Domain.ValueObjects;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Domain.ValueObjects;

public class NicknameTests
{
    [Theory]
    [InlineData("AB")]
    [InlineData("ValidNick")]
    [InlineData("12345678901234567890")]
    [UnitTest]
    public void Create_WithValidNickname_ReturnsSuccess(string validNickname)
    {
        // Act
        var result = Nickname.Create(validNickname);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(validNickname, result.Value.Value);
    }

    [Fact]
    [UnitTest]
    public void Create_WithWhitespace_TrimsAndSucceeds()
    {
        // Arrange
        var nickname = "  ValidNick  ";

        // Act
        var result = Nickname.Create(nickname);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ValidNick", result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [UnitTest]
    public void Create_WithEmptyNickname_ReturnsFailure(string invalidNickname)
    {
        // Act
        var result = Nickname.Create(invalidNickname);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public void Create_WithTooShortNickname_ReturnsFailure()
    {
        // Arrange
        var shortNickname = "A";

        // Act
        var result = Nickname.Create(shortNickname);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("at least 2 characters", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public void Create_WithTooLongNickname_ReturnsFailure()
    {
        // Arrange
        var longNickname = new string('A', 21);

        // Act
        var result = Nickname.Create(longNickname);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot exceed 20 characters", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var nickname = Nickname.Create("TestNick").Value;

        // Act
        string nicknameString = nickname;

        // Assert
        Assert.Equal("TestNick", nicknameString);
    }
}