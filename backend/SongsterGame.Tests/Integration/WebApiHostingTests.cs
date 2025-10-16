using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SongsterGame.Api.Services;
using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace SongsterGame.Tests.Integration;

/// <summary>
/// Integration tests to verify that the web API is properly configured,
/// all services are correctly registered, and endpoints are accessible.
/// </summary>
public class WebApiHostingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebApiHostingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Application_ShouldStart_WithoutErrors()
    {
        // This test verifies that the application can bootstrap successfully
        // by creating a client, which internally builds and starts the application
        Assert.NotNull(_client);
        Assert.NotNull(_factory.Services);
    }

    [Fact]
    public void ServiceRegistration_GameService_ShouldBeRegistered()
    {
        // Arrange & Act
        var gameService = _factory.Services.GetService<IGameService>();

        // Assert
        Assert.NotNull(gameService);
        Assert.IsAssignableFrom<IGameService>(gameService);
    }

    [Fact]
    public void ServiceRegistration_SpotifyService_ShouldBeRegistered()
    {
        // Arrange & Act
        var spotifyService = _factory.Services.GetService<ISpotifyService>();

        // Assert
        Assert.NotNull(spotifyService);
        Assert.IsAssignableFrom<ISpotifyService>(spotifyService);
    }

    [Fact]
    public void ServiceRegistration_HttpClient_ShouldBeRegistered()
    {
        // Arrange & Act
        var httpClientFactory = _factory.Services.GetService<IHttpClientFactory>();

        // Assert
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
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
    public async Task SignalRHub_ShouldBeAccessible()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            // Act
            await connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, connection.State);
        }
        finally
        {
            // Cleanup
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignalRHub_AfterConnection_ShouldAllowDisconnection()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            // Act
            await connection.StartAsync();
            await connection.StopAsync();

            // Assert
            Assert.Equal(HubConnectionState.Disconnected, connection.State);
        }
        finally
        {
            // Cleanup
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task OpenApiEndpoint_InDevelopment_ShouldBeAccessible()
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
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
    public void ServiceLifetime_GameService_ShouldBeSingleton()
    {
        // Arrange & Act
        var service1 = _factory.Services.GetService<IGameService>();
        var service2 = _factory.Services.GetService<IGameService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void ServiceLifetime_SpotifyService_ShouldBeSingleton()
    {
        // Arrange & Act
        var service1 = _factory.Services.GetService<ISpotifyService>();
        var service2 = _factory.Services.GetService<ISpotifyService>();

        // Assert
        Assert.Same(service1, service2);
    }

    #region CORS Policy Tests

    [Fact]
    public async Task CorsPolicy_ShouldAllowConfiguredOrigin()
    {
        // Arrange
        var origin = "http://localhost:5173";
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
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
    public async Task CorsPolicy_ShouldAllowCredentials()
    {
        // Arrange
        var origin = "http://localhost:5173";
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
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
    public async Task CorsPolicy_ShouldRejectUnauthorizedOrigin()
    {
        // Arrange
        var authorizedOrigin = "http://localhost:5173";
        var unauthorizedOrigin = "http://malicious-site.com";
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
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
    public async Task GameHub_CreateGame_ShouldReturnSuccess()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act
            var result = await connection.InvokeAsync<dynamic>("CreateGame", "TestPlayer");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.success);
            Assert.NotNull(result.gameCode);
            Assert.NotNull(result.players);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_CreateGame_ShouldFailIfGameAlreadyExists()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection1 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection1.StartAsync();
            await connection2.StartAsync();

            // Act - Create first game
            var result1 = await connection1.InvokeAsync<dynamic>("CreateGame", "Player1");

            // Act - Try to create second game (should fail in MVP - only one game allowed)
            var result2 = await connection2.InvokeAsync<dynamic>("CreateGame", "Player2");

            // Assert
            Assert.True(result1.success);
            Assert.False(result2.success);
            Assert.Contains("already exists", (string)result2.message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection1.DisposeAsync();
            await connection2.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_JoinGame_ShouldSucceedWithValidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var hostConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var playerConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await hostConnection.StartAsync();
            await playerConnection.StartAsync();

            // Act - Create game
            var createResult = await hostConnection.InvokeAsync<dynamic>("CreateGame", "Host");
            string gameCode = createResult.gameCode;

            // Act - Join game
            var joinResult = await playerConnection.InvokeAsync<dynamic>("JoinGame", gameCode, "Player2");

            // Assert
            Assert.NotNull(joinResult);
            Assert.True(joinResult.success);
            Assert.NotNull(joinResult.players);
            Assert.Equal(2, ((IEnumerable<dynamic>)joinResult.players).Count());
        }
        finally
        {
            await hostConnection.DisposeAsync();
            await playerConnection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_JoinGame_ShouldFailWithInvalidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act - Try to join non-existent game
            var result = await connection.InvokeAsync<dynamic>("JoinGame", "INVALID", "Player1");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.success);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_StartGame_ShouldSucceedWhenCalledByHost()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act - Create game
            var createResult = await connection.InvokeAsync<dynamic>("CreateGame", "Host");
            string gameCode = createResult.gameCode;

            // Act - Start game
            var startResult = await connection.InvokeAsync<dynamic>("StartGame", gameCode);

            // Assert
            Assert.NotNull(startResult);
            Assert.True(startResult.success);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_StartGame_ShouldFailWhenCalledByNonHost()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var hostConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var playerConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await hostConnection.StartAsync();
            await playerConnection.StartAsync();

            // Act - Create game and join with second player
            var createResult = await hostConnection.InvokeAsync<dynamic>("CreateGame", "Host");
            string gameCode = createResult.gameCode;
            await playerConnection.InvokeAsync<dynamic>("JoinGame", gameCode, "Player2");

            // Act - Try to start game from non-host connection
            var startResult = await playerConnection.InvokeAsync<dynamic>("StartGame", gameCode);

            // Assert
            Assert.NotNull(startResult);
            Assert.False(startResult.success);
            Assert.Contains("host", (string)startResult.message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await hostConnection.DisposeAsync();
            await playerConnection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GameHub_PlaceCard_ShouldReturnResult()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act - Create and start game
            var createResult = await connection.InvokeAsync<dynamic>("CreateGame", "Host");
            string gameCode = createResult.gameCode;
            await connection.InvokeAsync<dynamic>("StartGame", gameCode);

            // Act - Place card
            var placeResult = await connection.InvokeAsync<dynamic>("PlaceCard", gameCode, 0);

            // Assert
            Assert.NotNull(placeResult);
            Assert.True(placeResult.success);
            // isValid may be true or false depending on the card, but should be present
            Assert.NotNull(placeResult.isValid);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/non-existent-endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WithInvalidMethod_ShouldReturn405()
    {
        // Act
        var response = await _client.PostAsync("/health", null);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task SignalRHub_WithInvalidUrl_ShouldFailConnection()
    {
        // Arrange
        var invalidHubUrl = $"{_client.BaseAddress}invalidHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(invalidHubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await connection.StartAsync();
        });
    }

    [Fact]
    public async Task GameHub_PlaceCard_ShouldFailWithInvalidGameCode()
    {
        // Arrange
        var hubUrl = $"{_client.BaseAddress}gameHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act - Try to place card in non-existent game
            var result = await connection.InvokeAsync<dynamic>("PlaceCard", "INVALID", 0);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.success);
            Assert.Contains("not found", (string)result.message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    #endregion
}
