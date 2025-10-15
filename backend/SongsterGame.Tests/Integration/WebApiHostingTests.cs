using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SongsterGame.Api.Services;
using System.Net;

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
}
