namespace SongsterGame.Api.Application.DTOs.Common;

/// <summary>
/// Immutable DTO representing a music card.
/// </summary>
public sealed record MusicCardDto(
    string Title,
    string Artist,
    int Year,
    string PreviewUrl
);
