using FluentValidation;

namespace SongsterGame.Api.Application.Features.PlaceCard;

/// <summary>
/// Validator for PlaceCardCommand.
/// </summary>
public sealed class PlaceCardCommandValidator : AbstractValidator<PlaceCardCommand>
{
    public PlaceCardCommandValidator()
    {
        RuleFor(x => x.GameCode)
            .NotEmpty()
            .WithMessage("Game code cannot be empty")
            .Length(8)
            .WithMessage("Game code must be exactly 8 characters")
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Game code must contain only uppercase letters and digits");

        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID cannot be empty");

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Position cannot be negative");
    }
}
