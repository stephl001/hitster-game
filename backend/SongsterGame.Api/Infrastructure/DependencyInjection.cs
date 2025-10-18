using Microsoft.Extensions.DependencyInjection;
using SongsterGame.Api.Services;

namespace SongsterGame.Api.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure layer services including repositories and external services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register TimeProvider for testable time access
        services.AddSingleton(TimeProvider.System);

        // Register existing services (will be migrated incrementally)
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<ISpotifyService, SpotifyService>();

        // Register HttpClient for external API calls
        services.AddHttpClient();

        // Future: Add repository implementations here
        // services.AddSingleton<IGameRepository, InMemoryGameRepository>();

        return services;
    }
}