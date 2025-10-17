using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SongsterGame.Api.Application.DTOs.HubResults;
using SongsterGame.Api.Services;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit.Categories;

namespace SongsterGame.Tests.Integration;

/// <summary>
/// Integration tests to verify that the web API is properly configured,
/// all services are correctly registered, and endpoints are accessible.
/// </summary>
public class WebApiHostingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    [IntegrationTest]
    public void Application_ShouldStart_WithoutErrors()
    {
        // This test verifies that the application can bootstrap successfully
        // by creating a client, which internally builds and starts the application
        Assert.NotNull(_client);
        Assert.NotNull(factory.Services);
    }

    [Fact]
    [IntegrationTest]
    public void ServiceRegistration_GameService_ShouldBeRegistered()
    {
        // Arrange & Act
        var gameService = factory.Services.GetService<IGameService>();

        // Assert
        Assert.NotNull(gameService);
        Assert.IsAssignableFrom<IGameService>(gameService);
    }

    [Fact]
    [IntegrationTest]
    public void ServiceRegistration_SpotifyService_ShouldBeRegistered()
    {
        // Arrange & Act
        var spotifyService = factory.Services.GetService<ISpotifyService>();

        // Assert
        Assert.NotNull(spotifyService);
        Assert.IsAssignableFrom<ISpotifyService>(spotifyService);
    }

    [Fact]
    [IntegrationTest]
    public void ServiceRegistration_HttpClient_ShouldBeRegistered()
    {
        // Arrange & Act
        var httpClientFactory = factory.Services.GetService<IHttpClientFactory>();

        // Assert
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    [IntegrationTest]
    public async Task HealthEndpoint_ShouldReturn_HealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [IntegrationTest]
    public async Task SignalRHub_ShouldBeAccessible()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        // Act
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    [IntegrationTest]
    public async Task SignalRHub_AfterConnection_ShouldAllowDisconnection()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        // Act
        await connection.StartAsync();
        await connection.StopAsync();

        // Assert
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }

    [Fact]
    [IntegrationTest]
    public async Task OpenApiEndpoint_InDevelopment_ShouldBeAccessible()
    {
        // Arrange
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:Url"] = "http://localhost:5173"
                });
            });
        });
        var client = customFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        // Should return OK (200) in development environment
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [IntegrationTest]
    public void ServiceLifetime_GameService_ShouldBeSingleton()
    {
        // Arrange & Act
        var service1 = factory.Services.GetService<IGameService>();
        var service2 = factory.Services.GetService<IGameService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    [IntegrationTest]
    public void ServiceLifetime_SpotifyService_ShouldBeSingleton()
    {
        // Arrange & Act
        var service1 = factory.Services.GetService<ISpotifyService>();
        var service2 = factory.Services.GetService<ISpotifyService>();

        // Assert
        Assert.Same(service1, service2);
    }

    #region CORS Policy Tests

    [Fact]
    [IntegrationTest]
    public async Task CorsPolicy_ShouldAllowConfiguredOrigin()
    {
        // Arrange
        var origin = "http://localhost:5173";
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:Url"] = origin
                });
            });
        });
        var client = customFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        var allowOriginHeader = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal(origin, allowOriginHeader);
    }

    [Fact]
    [IntegrationTest]
    public async Task CorsPolicy_ShouldAllowCredentials()
    {
        // Arrange
        var origin = "http://localhost:5173";
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:Url"] = origin
                });
            });
        });
        var client = customFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        var allowCredentialsHeader = response.Headers.GetValues("Access-Control-Allow-Credentials").FirstOrDefault();
        Assert.Equal("true", allowCredentialsHeader);
    }

    [Fact]
    [IntegrationTest]
    public async Task CorsPolicy_ShouldRejectUnauthorizedOrigin()
    {
        // Arrange
        var authorizedOrigin = "http://localhost:5173";
        var unauthorizedOrigin = "http://malicious-site.com";
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:Url"] = authorizedOrigin
                });
            });
        });
        var client = customFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", unauthorizedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // Should not include Access-Control-Allow-Origin header for unauthorized origin
        var allowOriginHeader = response.Headers.Contains("Access-Control-Allow-Origin")
            ? response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault()
            : null;
        Assert.NotEqual(unauthorizedOrigin, allowOriginHeader);
    }

    #endregion

    #region SignalR Hub Method Tests

    [Fact]
    [IntegrationTest]
    public async Task GameHub_CreateGame_ShouldReturnSuccess()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection.StartAsync();

        // Act
        var result = await connection.InvokeAsync<HubResult>("CreateGame", "TestPlayer");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        var successResult = Assert.IsType<CreateGameSuccessHubResult>(result);
        Assert.NotNull(successResult.GameCode);
        Assert.NotEmpty(successResult.Players);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_CreateGame_ShouldFailIfGameAlreadyExists()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection1 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await using var connection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection1.StartAsync();
        await connection2.StartAsync();

        // Act - Create first game
        var result1 = await connection1.InvokeAsync<HubResult>("CreateGame", "Player1");

        // Act - Try to create second game (should fail in MVP - only one game allowed)
        var result2 = await connection2.InvokeAsync<HubResult>("CreateGame", "Player2");

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        var failureResult = Assert.IsType<FailureHubResult>(result2);
        Assert.Contains("already exists", failureResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_JoinGame_ShouldSucceedWithValidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var hostConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await using var playerConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await hostConnection.StartAsync();
        await playerConnection.StartAsync();

        // Act - Create game
        var createResult = await hostConnection.InvokeAsync<HubResult>("CreateGame", "Host");
        var createSuccess = Assert.IsType<CreateGameSuccessHubResult>(createResult);
        string gameCode = createSuccess.GameCode;

        // Act - Join game
        var joinResult = await playerConnection.InvokeAsync<HubResult>("JoinGame", gameCode, "Player2");

        // Assert
        Assert.NotNull(joinResult);
        Assert.True(joinResult.Success);
        var joinSuccess = Assert.IsType<JoinGameSuccessHubResult>(joinResult);
        Assert.Equal(2, joinSuccess.Players.Count);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_JoinGame_ShouldFailWithInvalidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection.StartAsync();

        // Act - Try to join non-existent game
        var result = await connection.InvokeAsync<HubResult>("JoinGame", "INVALID", "Player1");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.IsType<FailureHubResult>(result);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_StartGame_ShouldSucceedWhenCalledByHost()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection.StartAsync();

        // Act - Create game
        var createResult = await connection.InvokeAsync<HubResult>("CreateGame", "Host");
        var createSuccess = Assert.IsType<CreateGameSuccessHubResult>(createResult);
        string gameCode = createSuccess.GameCode;
        await connection.InvokeAsync<HubResult>("JoinGame", gameCode, "Player2");

        // Act - Start game
        var startResult = await connection.InvokeAsync<HubResult>("StartGame", gameCode);

        // Assert
        Assert.NotNull(startResult);
        Assert.True(startResult.Success);
        Assert.IsType<StartGameSuccessHubResult>(startResult);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_StartGame_ShouldFailWhenCalledByNonHost()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var hostConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await using var playerConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await hostConnection.StartAsync();
        await playerConnection.StartAsync();

        // Act - Create game and join with second player
        var createResult = await hostConnection.InvokeAsync<HubResult>("CreateGame", "Host");
        var createSuccess = Assert.IsType<CreateGameSuccessHubResult>(createResult);
        string gameCode = createSuccess.GameCode;
        await playerConnection.InvokeAsync<HubResult>("JoinGame", gameCode, "Player2");

        // Act - Try to start game from non-host connection
        var startResult = await playerConnection.InvokeAsync<HubResult>("StartGame", gameCode);

        // Assert
        Assert.NotNull(startResult);
        Assert.False(startResult.Success);
        var failureResult = Assert.IsType<FailureHubResult>(startResult);
        Assert.Contains("host", failureResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_PlaceCard_ShouldReturnResult()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection.StartAsync();

        // Act - Create game and add second player
        var createResult = await connection.InvokeAsync<HubResult>("CreateGame", "Host");
        var createSuccess = Assert.IsType<CreateGameSuccessHubResult>(createResult);
        string gameCode = createSuccess.GameCode;
        await connection.InvokeAsync<HubResult>("JoinGame", gameCode, "Player2");

        // Act - Start game
        await connection.InvokeAsync<HubResult>("StartGame", gameCode);

        // Act - Place card
        var placeResult = await connection.InvokeAsync<HubResult>("PlaceCard", gameCode, 0);

        // Assert
        Assert.NotNull(placeResult);
        Assert.True(placeResult.Success);
        var placeSuccess = Assert.IsType<PlaceCardSuccessHubResult>(placeResult);
        // isValid may be true or false depending on the card - we just verify it's returned
        // (No need to assert on bool value type)
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    [IntegrationTest]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/non-existent-endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [IntegrationTest]
    public async Task HealthEndpoint_WithInvalidMethod_ShouldReturn405()
    {
        // Act
        var response = await _client.PostAsync("/health", null);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    [IntegrationTest]
    public async Task SignalRHub_WithInvalidUrl_ShouldFailConnection()
    {
        // Arrange
        var invalidHubUrl = $"{_client.BaseAddress}invalidHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(invalidHubUrl,
                options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => { await connection.StartAsync(); });
    }

    [Fact]
    [IntegrationTest]
    public async Task GameHub_PlaceCard_ShouldFailWithInvalidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler(); })
            .Build();

        await connection.StartAsync();

        // Act - Try to place card in non-existent game (use valid 8-char code that doesn't exist)
        var result = await connection.InvokeAsync<HubResult>("PlaceCard", "INVALID1", 0);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        var failureResult = Assert.IsType<FailureHubResult>(result);
        Assert.Contains("not found", failureResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}