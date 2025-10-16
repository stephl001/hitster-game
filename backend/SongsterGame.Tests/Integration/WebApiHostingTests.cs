using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SongsterGame.Api.Services;
using System.Net;
using System.Text.Json.Nodes;
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
        var result = await connection.InvokeAsync<JsonObject>("CreateGame", "TestPlayer");

        // Assert
        Assert.NotNull(result);
        Assert.True(result["success"]?.GetValue<bool>());
        Assert.NotNull(result["gameCode"]?.GetValue<string>());
        Assert.NotEmpty(result["players"]?.AsArray() ?? []);
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
        var result1 = await connection1.InvokeAsync<JsonObject>("CreateGame", "Player1");

        // Act - Try to create second game (should fail in MVP - only one game allowed)
        var result2 = await connection2.InvokeAsync<JsonObject>("CreateGame", "Player2");

        // Assert
        Assert.True(result1["success"]?.GetValue<bool>());
        Assert.False(result2["success"]?.GetValue<bool>());
        Assert.Contains("already exists", result2["message"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
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
        var createResult = await hostConnection.InvokeAsync<JsonObject>("CreateGame", "Host");
        string gameCode = createResult["gameCode"]!.GetValue<string>();

        // Act - Join game
        var joinResult = await playerConnection.InvokeAsync<JsonObject>("JoinGame", gameCode, "Player2");

        // Assert
        Assert.NotNull(joinResult);
        Assert.True(joinResult["success"]?.GetValue<bool>());
        Assert.Equal(2, (joinResult["players"]?.AsArray() ?? []).Count);
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
        var result = await connection.InvokeAsync<JsonObject>("JoinGame", "INVALID", "Player1");

        // Assert
        Assert.NotNull(result);
        Assert.False(result["success"]?.GetValue<bool>());
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
        var createResult = await connection.InvokeAsync<JsonObject>("CreateGame", "Host");
        string gameCode = createResult["gameCode"]!.GetValue<string>();
        await connection.InvokeAsync<JsonObject>("JoinGame", gameCode, "Player2");

        // Act - Start game
        var startResult = await connection.InvokeAsync<JsonObject>("StartGame", gameCode);

        // Assert
        Assert.NotNull(startResult);
        Assert.True(startResult["success"]?.GetValue<bool>());
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
        var createResult = await hostConnection.InvokeAsync<JsonObject>("CreateGame", "Host");
        string gameCode = createResult["gameCode"]!.GetValue<string>();
        await playerConnection.InvokeAsync<JsonObject>("JoinGame", gameCode, "Player2");

        // Act - Try to start game from non-host connection
        var startResult = await playerConnection.InvokeAsync<JsonObject>("StartGame", gameCode);

        // Assert
        Assert.NotNull(startResult);
        Assert.False(startResult["success"]?.GetValue<bool>());
        Assert.Contains("host", startResult["message"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
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

        // Act - Create and start game
        var createResult = await connection.InvokeAsync<JsonObject>("CreateGame", "Host");
        string gameCode = createResult["gameCode"]!.GetValue<string>();
        await connection.InvokeAsync<JsonObject>("StartGame", gameCode);

        // Act - Place card
        var placeResult = await connection.InvokeAsync<JsonObject>("PlaceCard", gameCode, 0);

        // Assert
        Assert.NotNull(placeResult);
        Assert.True(placeResult["success"]?.GetValue<bool>());
        // isValid may be true or false depending on the card, but should be present
        Assert.NotNull(placeResult["isValid"]?.GetValue<bool>());
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

        // Act - Try to place card in non-existent game
        var result = await connection.InvokeAsync<JsonObject>("PlaceCard", "INVALID", 0);

        // Assert
        Assert.NotNull(result);
        Assert.False(result["success"]?.GetValue<bool>());
        Assert.Contains("not found", result["message"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}