using NSubstitute;
using SongsterGame.Api.Application.Features.CreateGame;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Application.Features.CreateGame;

public class CreateGameHandlerTests
{
    private readonly IGameService _gameService;
    private readonly CreateGameHandler _handler;

    public CreateGameHandlerTests()
    {
        _gameService = Substitute.For<IGameService>();
        _handler = new CreateGameHandler(_gameService);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateGameCommand("connection-123", "TestPlayer");
        var mockGame = new Game(TimeProvider.System)
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "TestPlayer",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };
        _gameService.CreateGame(Arg.Any<string>(), Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABCD1234", result.Value.GameCode);
        Assert.Single(result.Value.Players);
        Assert.Equal("TestPlayer", result.Value.Players[0].Nickname);
        Assert.True(result.Value.Players[0].IsHost);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameAlreadyExists_ReturnsConflictError()
    {
        // Arrange
        var command = new CreateGameCommand("connection-123", "TestPlayer");
        _gameService.CreateGame(Arg.Any<string>(), Arg.Any<string>()).Returns((Game?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
        Assert.Contains("already exists", result.Error.Message);
    }

    [Theory]
    [InlineData("", "TestPlayer")]
    [InlineData("   ", "TestPlayer")]
    [UnitTest]
    public async Task Handle_WithInvalidConnectionId_ReturnsValidationError(
        string invalidConnectionId,
        string nickname)
    {
        // Arrange
        var command = new CreateGameCommand(invalidConnectionId, nickname);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("connection-123", "")]
    [InlineData("connection-123", "A")]
    [InlineData("connection-123", "123456789012345678901")]
    [UnitTest]
    public async Task Handle_WithInvalidNickname_ReturnsValidationError(
        string connectionId,
        string invalidNickname)
    {
        // Arrange
        var command = new CreateGameCommand(connectionId, invalidNickname);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }
}