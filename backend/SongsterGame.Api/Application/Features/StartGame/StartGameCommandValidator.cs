using FluentValidation;

namespace SongsterGame.Api.Application.Features.StartGame;

/// <summary>
/// Validator for StartGameCommand.
/// </summary>
public sealed class StartGameCommandValidator : AbstractValidator<StartGameCommand>
{
    public StartGameCommandValidator()
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
    }
}
