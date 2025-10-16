using Bogus;
using NSubstitute;
using SongsterGame.Api.Models;
using SongsterGame.Api.Services;
using Xunit.Categories;

namespace SongsterGame.Tests.Unit.Services;

public class GameServiceTests
{
    private readonly ISpotifyService _spotifyService;
    private readonly GameService _gameService;
    private readonly Faker _faker;
    private readonly Faker<MusicCard> _musicCardFaker;

    public GameServiceTests()
    {
        // Setup mocks
        _spotifyService = Substitute.For<ISpotifyService>();
        _gameService = new GameService(_spotifyService);

        // Setup Bogus fakers for test data
        _faker = new Faker();
        _musicCardFaker = new Faker<MusicCard>()
            .RuleFor(c => c.SpotifyId, f => f.Random.Guid().ToString())
            .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
            .RuleFor(c => c.Artist, f => f.Name.FullName())
            .RuleFor(c => c.Year, f => f.Random.Int(1950, 2024))
            .RuleFor(c => c.PreviewUrl, f => f.Internet.Url());
    }

    [Fact]
    [UnitTest]
    public void CreateGame_WhenNoGameExists_ReturnsNewGame()
    {
        // Arrange
        var connectionId = _faker.Random.Guid().ToString();
        var nickname = _faker.Internet.UserName();

        // Act
        var game = _gameService.CreateGame(connectionId, nickname);

        // Assert
        Assert.NotNull(game);
        Assert.NotEmpty(game.GameCode);
        Assert.Single(game.Players);
        Assert.Equal(nickname, game.Players[0].Nickname);
        Assert.True(game.Players[0].IsHost);
        Assert.Equal(GameState.Lobby, game.State);
    }

    [Fact]
    [UnitTest]
    public void CreateGame_WhenGameAlreadyExists_ReturnsNull()
    {
        // Arrange
        var connectionId1 = _faker.Random.Guid().ToString();
        var nickname1 = _faker.Internet.UserName();
        var connectionId2 = _faker.Random.Guid().ToString();
        var nickname2 = _faker.Internet.UserName();
        _gameService.CreateGame(connectionId1, nickname1);

        // Act
        var secondGame = _gameService.CreateGame(connectionId2, nickname2);

        // Assert
        Assert.Null(secondGame);
    }

    [Fact]
    [UnitTest]
    public void GenerateGameCode_ReturnsEightCharacterCode()
    {
        // Arrange & Act
        var gameCode = _gameService.GenerateGameCode();

        // Assert
        Assert.Equal(8, gameCode.Length);
        Assert.All(gameCode, c => Assert.True(char.IsLetterOrDigit(c)));
        Assert.All(gameCode, c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_ValidPlacementBetweenCards_ReturnsTrue()
    {
        // Arrange
        var timeline = new List<MusicCard>
        {
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2000).Generate(),
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2020).Generate()
        };
        var card = _musicCardFaker.Clone().RuleFor(c => c.Year, 2010).Generate();
        var position = 1;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_InvalidPlacement_YearTooEarly_ReturnsFalse()
    {
        // Arrange
        var timeline = new List<MusicCard>
        {
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2000).Generate(),
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2020).Generate()
        };
        var card = _musicCardFaker.Clone().RuleFor(c => c.Year, 1990).Generate();
        var position = 1;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_InvalidPlacement_YearTooLate_ReturnsFalse()
    {
        // Arrange
        var timeline = new List<MusicCard>
        {
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2000).Generate(),
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2020).Generate()
        };
        var card = _musicCardFaker.Clone().RuleFor(c => c.Year, 2025).Generate();
        var position = 1;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_FirstCardInTimeline_ReturnsTrue()
    {
        // Arrange
        var timeline = new List<MusicCard>();
        var card = _musicCardFaker.Generate();
        var position = 0;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_AtStartOfTimeline_ValidYear_ReturnsTrue()
    {
        // Arrange
        var timeline = new List<MusicCard>
        {
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2010).Generate()
        };
        var card = _musicCardFaker.Clone().RuleFor(c => c.Year, 2000).Generate();
        var position = 0;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [UnitTest]
    public void ValidatePlacement_AtEndOfTimeline_ValidYear_ReturnsTrue()
    {
        // Arrange
        var timeline = new List<MusicCard>
        {
            _musicCardFaker.Clone().RuleFor(c => c.Year, 2000).Generate()
        };
        var card = _musicCardFaker.Clone().RuleFor(c => c.Year, 2010).Generate();
        var position = 1;

        // Act
        var result = _gameService.ValidatePlacement(timeline, card, position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [UnitTest]
    public void JoinGame_ValidGameCode_AddsPlayer()
    {
        // Arrange
        var hostConnectionId = _faker.Random.Guid().ToString();
        var hostNickname = _faker.Internet.UserName();
        var game = _gameService.CreateGame(hostConnectionId, hostNickname);

        var playerConnectionId = _faker.Random.Guid().ToString();
        var playerNickname = _faker.Internet.UserName();

        // Act
        var result = _gameService.JoinGame(game!.GameCode, playerConnectionId, playerNickname);

        // Assert
        Assert.True(result);
        Assert.Equal(2, game.Players.Count);
        Assert.Contains(game.Players, p => p.Nickname == playerNickname && !p.IsHost);
    }

    [Fact]
    [UnitTest]
    public void JoinGame_DuplicateNickname_ReturnsFalse()
    {
        // Arrange
        var hostConnectionId = _faker.Random.Guid().ToString();
        var nickname = _faker.Internet.UserName();
        var game = _gameService.CreateGame(hostConnectionId, nickname);

        var playerConnectionId = _faker.Random.Guid().ToString();

        // Act
        var result = _gameService.JoinGame(game!.GameCode, playerConnectionId, nickname);

        // Assert
        Assert.False(result);
        Assert.Single(game.Players);
    }

    [Fact]
    [UnitTest]
    public void JoinGame_GameFull_ReturnsFalse()
    {
        // Arrange
        var hostConnectionId = _faker.Random.Guid().ToString();
        var hostNickname = _faker.Internet.UserName();
        var game = _gameService.CreateGame(hostConnectionId, hostNickname);

        // Add players up to max capacity
        for (int i = 1; i < Game.MaxPlayers; i++)
        {
            _gameService.JoinGame(game!.GameCode, _faker.Random.Guid().ToString(), _faker.Internet.UserName());
        }

        var extraPlayerConnectionId = _faker.Random.Guid().ToString();
        var extraPlayerNickname = _faker.Internet.UserName();

        // Act
        var result = _gameService.JoinGame(game!.GameCode, extraPlayerConnectionId, extraPlayerNickname);

        // Assert
        Assert.False(result);
        Assert.Equal(Game.MaxPlayers, game.Players.Count);
    }
}
