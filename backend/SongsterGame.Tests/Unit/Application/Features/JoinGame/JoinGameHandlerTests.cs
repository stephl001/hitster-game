using NSubstitute;
using SongsterGame.Api.Application.Features.JoinGame;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Application.Features.JoinGame;

public class JoinGameHandlerTests
{
    private readonly IGameService _gameService;
    private readonly JoinGameHandler _handler;

    public JoinGameHandlerTests()
    {
        _gameService = Substitute.For<IGameService>();
        _handler = new JoinGameHandler(_gameService);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "Player2");
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };

        var updatedGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-456",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };

        // Setup GetGame to return mockGame first, then updatedGame on second call
        _gameService.GetGame(gameCode).Returns(mockGame, updatedGame);
        _gameService.JoinGame(gameCode, "connection-456", "Player2").Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(gameCode, result.Value.GameCode);
        Assert.Equal(2, result.Value.Players.Count);
        Assert.Contains(result.Value.Players, p => p.Nickname == "Player2" && !p.IsHost);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithInvalidGameCode_ReturnsNotFoundError()
    {
        // Arrange
        var command = new JoinGameCommand("INVALID1", "connection-456", "Player2");
        _gameService.GetGame(Arg.Any<string>()).Returns((Game?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
        Assert.Contains("not found", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameAlreadyStarted_ReturnsBusinessRuleError()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "Player2");
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Playing // Game already started
        };

        _gameService.GetGame(gameCode).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("already started", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameIsFull_ReturnsBusinessRuleError()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "Player5");
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player { ConnectionId = "c1", Nickname = "P1", IsHost = true },
                new Player { ConnectionId = "c2", Nickname = "P2", IsHost = false },
                new Player { ConnectionId = "c3", Nickname = "P3", IsHost = false },
                new Player { ConnectionId = "c4", Nickname = "P4", IsHost = false }
            ],
            State = GameState.Lobby
        };

        _gameService.GetGame(gameCode).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("full", result.Error.Message);
        Assert.Contains("4", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenNicknameAlreadyExists_ReturnsConflictError()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "Player1"); // Same nickname
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };

        _gameService.GetGame(gameCode).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
        Assert.Contains("already exists", result.Error.Message);
    }

    [Theory]
    [InlineData("", "connection-456", "Player2")]
    [InlineData("   ", "connection-456", "Player2")]
    [InlineData("ABC", "connection-456", "Player2")] // Too short
    [InlineData("ABCDEFGHI", "connection-456", "Player2")] // Too long
    [UnitTest]
    public async Task Handle_WithInvalidGameCode_ReturnsValidationError(
        string invalidGameCode,
        string connectionId,
        string nickname)
    {
        // Arrange
        var command = new JoinGameCommand(invalidGameCode, connectionId, nickname);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("ABCD1234", "", "Player2")]
    [InlineData("ABCD1234", "   ", "Player2")]
    [UnitTest]
    public async Task Handle_WithInvalidConnectionId_ReturnsValidationError(
        string gameCode,
        string invalidConnectionId,
        string nickname)
    {
        // Arrange
        var command = new JoinGameCommand(gameCode, invalidConnectionId, nickname);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("ABCD1234", "connection-456", "")]
    [InlineData("ABCD1234", "connection-456", "A")] // Too short
    [InlineData("ABCD1234", "connection-456", "123456789012345678901")] // Too long
    [UnitTest]
    public async Task Handle_WithInvalidNickname_ReturnsValidationError(
        string gameCode,
        string connectionId,
        string invalidNickname)
    {
        // Arrange
        var command = new JoinGameCommand(gameCode, connectionId, invalidNickname);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameServiceJoinFails_ReturnsBusinessRuleError()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "Player2");
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };

        _gameService.GetGame(gameCode).Returns(mockGame);
        _gameService.JoinGame(gameCode, "connection-456", "Player2").Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("unexpected error", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_GameCodeIsNormalized_ToUpperCase()
    {
        // Arrange
        var command = new JoinGameCommand("abcd1234", "connection-456", "Player2");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };

        var updatedGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player { ConnectionId = "connection-123", Nickname = "Player1", IsHost = true },
                new Player { ConnectionId = "connection-456", Nickname = "Player2", IsHost = false }
            ],
            State = GameState.Lobby
        };

        _gameService.GetGame("ABCD1234").Returns(mockGame, updatedGame);
        _gameService.JoinGame("ABCD1234", "connection-456", "Player2").Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ABCD1234", result.Value.GameCode);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_NicknameIsNormalized_TrimmedWhitespace()
    {
        // Arrange
        var gameCode = "ABCD1234";
        var command = new JoinGameCommand(gameCode, "connection-456", "  Player2  ");
        var mockGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-123",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };

        var updatedGame = new Game
        {
            GameCode = gameCode,
            Players =
            [
                new Player { ConnectionId = "connection-123", Nickname = "Player1", IsHost = true },
                new Player { ConnectionId = "connection-456", Nickname = "Player2", IsHost = false }
            ],
            State = GameState.Lobby
        };

        _gameService.GetGame(gameCode).Returns(mockGame, updatedGame);
        _gameService.JoinGame(gameCode, "connection-456", "Player2").Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Players, p => p.Nickname == "Player2");
    }
}