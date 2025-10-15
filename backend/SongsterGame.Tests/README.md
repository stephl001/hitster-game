# SongsterGame Backend Tests

This directory contains unit and integration tests for the Songster Game backend API.

## Testing Framework

- **xUnit**: Testing framework and assertions
- **Bogus**: Random test data generation
- **NSubstitute**: Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing for ASP.NET Core
- **Microsoft.AspNetCore.SignalR.Client**: SignalR hub testing

## Project Structure

```
SongsterGame.Tests/
├── Unit/
│   ├── Services/
│   │   ├── GameServiceTests.cs       # Tests for game logic
│   │   └── SpotifyServiceTests.cs    # Tests for Spotify integration (when implemented)
│   └── Models/
│       ├── GameTests.cs              # Tests for Game model
│       └── PlayerTests.cs            # Tests for Player model
└── Integration/
    ├── Hubs/
    │   └── GameHubTests.cs           # Tests for SignalR GameHub
    └── GameFlowTests.cs              # End-to-end game flow tests
```

## Testing Patterns

All tests follow the **Arrange-Act-Assert (AAA)** pattern:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var service = new GameService();
    var testData = CreateTestData();

    // Act - Execute the method being tested
    var result = service.MethodUnderTest(testData);

    // Assert - Verify the expected outcome
    Assert.Equal(expectedValue, result);
}
```

## Using Bogus for Test Data

Bogus generates realistic random data for tests:

```csharp
// Setup faker in constructor
private readonly Faker<MusicCard> _musicCardFaker;

public GameServiceTests()
{
    _musicCardFaker = new Faker<MusicCard>()
        .RuleFor(c => c.SpotifyId, f => f.Random.Guid().ToString())
        .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
        .RuleFor(c => c.Artist, f => f.Name.FullName())
        .RuleFor(c => c.Year, f => f.Random.Int(1950, 2024))
        .RuleFor(c => c.PreviewUrl, f => f.Internet.Url());
}

// Use in tests
var card = _musicCardFaker.Generate(); // Generate one
var cards = _musicCardFaker.Generate(10); // Generate multiple

// Override specific properties
var card2010 = _musicCardFaker
    .Clone()
    .RuleFor(c => c.Year, 2010)
    .Generate();
```

## Using NSubstitute for Mocking

NSubstitute creates test doubles for dependencies:

```csharp
// Create a mock
var spotifyService = Substitute.For<ISpotifyService>();

// Setup return values
spotifyService.GetPlaylist(Arg.Any<string>())
    .Returns(mockPlaylistData);

// Verify method was called
spotifyService.Received().GetPlaylist("playlistId");

// Verify method was NOT called
spotifyService.DidNotReceive().DeletePlaylist(Arg.Any<string>());
```

## Running Tests

### Run all tests
```bash
cd backend
dotnet test
```

### Run with detailed output
```bash
dotnet test --verbosity normal
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~GameServiceTests"
```

### Run specific test method
```bash
dotnet test --filter "FullyQualifiedName~GameServiceTests.CreateGame_WhenNoGameExists_ReturnsNewGame"
```

### Run with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Watch mode (re-run on file changes)
```bash
dotnet watch test
```

## Using the Test Agent

You can invoke the backend testing agent using the `/test-backend` slash command:

```
/test-backend
```

The agent will:
1. Check if the test project exists (create if needed)
2. Run all tests
3. Analyze and report results
4. Offer to fix failures or generate new tests

## Test Naming Convention

Follow this pattern for test method names:

```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `CreateGame_WhenNoGameExists_ReturnsNewGame`
- `JoinGame_DuplicateNickname_ReturnsFalse`
- `ValidatePlacement_YearTooEarly_ReturnsFalse`

## Integration Testing

For SignalR hub tests, use the `WebApplicationFactory`:

```csharp
public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test_SignalRHub()
    {
        // Arrange
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/gameHub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();

        // Act & Assert
        await connection.InvokeAsync("CreateGame", "TestHost");
    }
}
```

## Best Practices

1. **Keep tests isolated**: Each test should be independent
2. **Use descriptive names**: Test names should clearly indicate what they test
3. **Follow AAA pattern**: Separate Arrange, Act, and Assert with blank lines
4. **Use Bogus for data**: Don't hardcode test data when random data works
5. **Mock external dependencies**: Don't make real API calls in unit tests
6. **Test edge cases**: Empty lists, null values, boundary conditions
7. **One assertion per test**: Focus each test on a single behavior
8. **Fast tests**: Unit tests should run in milliseconds

## Example: Complete Test File

See `Unit/Services/GameServiceTests.cs` for a complete example demonstrating:
- Constructor setup with Bogus and NSubstitute
- Multiple test scenarios
- Proper AAA pattern
- Edge case testing
- Meaningful test names

## CI/CD Integration

Tests run automatically in GitHub Actions:
- On every push to `main` (dev environment)
- On tag push for production releases
- Pull requests require all tests to pass

## Troubleshooting

### Tests fail with "connection string" errors
- Check your appsettings.json test configuration
- Ensure test database is set up correctly

### SignalR connection errors in integration tests
- Verify WebApplicationFactory is properly configured
- Check that the test server is running

### Bogus data causes test failures
- Use `.RuleFor()` to override specific properties with deterministic values
- Use `Faker` seed for reproducible random data: `new Faker().UseSeed(1234)`

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Bogus Documentation](https://github.com/bchavez/Bogus)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [ASP.NET Core Testing](https://learn.microsoft.com/en-us/aspnet/core/test/)
