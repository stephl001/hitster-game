using NSubstitute;
using SongsterGame.Api.Application.Features.StartGame;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Application.Features.StartGame;

public class StartGameHandlerTests
{
    private readonly IGameService _gameService;
    private readonly StartGameHandler _handler;

    public StartGameHandlerTests()
    {
        _gameService = Substitute.For<IGameService>();
        _handler = new StartGameHandler(_gameService);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };
        var startedGame = new Game
        {
            GameCode = "ABCD1234",
            Players = mockGame.Players,
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard
            {
                Title = "Test Song",
                Artist = "Test Artist",
                Year = 2020
            }
        };

        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame, startedGame);
        _gameService.StartGame(Arg.Any<string>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("HostPlayer", result.Value.CurrentPlayerNickname);
        Assert.Equal("Test Song", result.Value.CurrentCardTitle);
        _gameService.Received(1).StartGame("ABCD1234");
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new StartGameCommand("INVALID1", "connection-123");
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
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Playing // Already started
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("already been started", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenCalledByNonHost_ReturnsUnauthorizedError()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-player2");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Code);
        Assert.Contains("Only the host", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenNoHostFound_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = false // No host
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("No host found", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithLessThanTwoPlayers_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                }
            ],
            State = GameState.Lobby
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("At least 2 players", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenStartGameFails_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);
        _gameService.StartGame(Arg.Any<string>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("Failed to start game", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameDisappearsAfterStart_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", "connection-host");
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-host",
                    Nickname = "HostPlayer",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Lobby
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame, (Game?)null);
        _gameService.StartGame(Arg.Any<string>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("Game disappeared", result.Error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCD123")] // Too short
    [InlineData("ABCD12345")] // Too long
    [InlineData("ABCD-123")] // Invalid character
    [InlineData("ABCD 123")] // Invalid character (space)
    [UnitTest]
    public async Task Handle_WithInvalidGameCode_ReturnsValidationError(string invalidGameCode)
    {
        // Arrange
        var command = new StartGameCommand(invalidGameCode, "connection-host");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [UnitTest]
    public async Task Handle_WithInvalidConnectionId_ReturnsValidationError(string invalidConnectionId)
    {
        // Arrange
        var command = new StartGameCommand("ABCD1234", invalidConnectionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }
}
