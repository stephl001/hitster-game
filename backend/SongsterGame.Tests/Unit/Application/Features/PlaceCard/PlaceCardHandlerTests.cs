using NSubstitute;
using SongsterGame.Api.Application.Features.PlaceCard;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Application.Features.PlaceCard;

public class PlaceCardHandlerTests
{
    private readonly IGameService _gameService;
    private readonly PlaceCardHandler _handler;

    public PlaceCardHandlerTests()
    {
        _gameService = Substitute.For<IGameService>();
        _handler = new PlaceCardHandler(_gameService);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithValidPlacement_ReturnsSuccess()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true,
                    Timeline = []
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard
            {
                Title = "Test Song",
                Artist = "Test Artist",
                Year = 2020,
                PreviewUrl = "https://test.com"
            }
        };

        var updatedGame = new Game
        {
            GameCode = "ABCD1234",
            Players = mockGame.Players,
            State = GameState.Playing,
            CurrentTurnIndex = 1,
            CurrentCard = new MusicCard
            {
                Title = "Next Song",
                Artist = "Next Artist",
                Year = 2021,
                PreviewUrl = "https://test.com"
            }
        };

        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame, updatedGame);
        _gameService.PlaceCard(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        _gameService.GetWinner(Arg.Any<string>()).Returns((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.False(result.Value.GameFinished);
        Assert.Equal("Player1", result.Value.PlayerNickname);
        Assert.Equal("Player2", result.Value.CurrentPlayerNickname);
        _gameService.Received(1).PlaceCard("ABCD1234", "connection-player1", 0);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithInvalidPlacement_ReturnsSuccessButInvalid()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true,
                    Timeline = []
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard
            {
                Title = "Test Song",
                Artist = "Test Artist",
                Year = 2020,
                PreviewUrl = "https://test.com"
            }
        };

        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);
        _gameService.PlaceCard(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()).Returns(false);
        _gameService.GetWinner(Arg.Any<string>()).Returns((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.False(result.Value.GameFinished);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WithWinningPlacement_ReturnsSuccessWithGameFinished()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true,
                    Timeline = []
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard
            {
                Title = "Test Song",
                Artist = "Test Artist",
                Year = 2020,
                PreviewUrl = "https://test.com"
            }
        };

        var winner = mockGame.Players[0];

        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);
        _gameService.PlaceCard(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        _gameService.GetWinner(Arg.Any<string>()).Returns(winner);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.True(result.Value.GameFinished);
        Assert.Equal("Player1", result.Value.WinnerNickname);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new PlaceCardCommand("INVALID1", "connection-player1", 0);
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
    public async Task Handle_WhenGameNotPlaying_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
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
        Assert.Contains("not in playing state", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenNoCurrentCard_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Playing,
            CurrentCard = null
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("No card is currently drawn", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenPlayerNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-unknown", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true
                }
            ],
            State = GameState.Playing,
            CurrentCard = new MusicCard { Title = "Test", Artist = "Artist", Year = 2020, PreviewUrl = "url" }
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
        Assert.Contains("Player not found", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenNotPlayerTurn_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player2", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true
                },
                new Player
                {
                    ConnectionId = "connection-player2",
                    Nickname = "Player2",
                    IsHost = false
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0, // Player1's turn
            CurrentCard = new MusicCard { Title = "Test", Artist = "Artist", Year = 2020, PreviewUrl = "url" }
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("not your turn", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenPositionOutOfRange_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 5); // Position > timeline length
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true,
                    Timeline = [] // Empty timeline
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard { Title = "Test", Artist = "Artist", Year = 2020, PreviewUrl = "url" }
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BusinessRule", result.Error.Code);
        Assert.Contains("out of range", result.Error.Message);
    }

    [Fact]
    [UnitTest]
    public async Task Handle_WhenGameDisappearsAfterPlacement_ReturnsBusinessRuleError()
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", 0);
        var mockGame = new Game
        {
            GameCode = "ABCD1234",
            Players =
            [
                new Player
                {
                    ConnectionId = "connection-player1",
                    Nickname = "Player1",
                    IsHost = true,
                    Timeline = []
                }
            ],
            State = GameState.Playing,
            CurrentTurnIndex = 0,
            CurrentCard = new MusicCard { Title = "Test", Artist = "Artist", Year = 2020, PreviewUrl = "url" }
        };
        _gameService.GetGame(Arg.Any<string>()).Returns(mockGame, (Game?)null);
        _gameService.PlaceCard(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        _gameService.GetWinner(Arg.Any<string>()).Returns((Player?)null);

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
    [UnitTest]
    public async Task Handle_WithInvalidGameCode_ReturnsValidationError(string invalidGameCode)
    {
        // Arrange
        var command = new PlaceCardCommand(invalidGameCode, "connection-player1", 0);

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
        var command = new PlaceCardCommand("ABCD1234", invalidConnectionId, 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [UnitTest]
    public async Task Handle_WithNegativePosition_ReturnsValidationError(int invalidPosition)
    {
        // Arrange
        var command = new PlaceCardCommand("ABCD1234", "connection-player1", invalidPosition);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }
}
