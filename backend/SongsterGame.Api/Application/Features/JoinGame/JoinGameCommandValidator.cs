using FluentValidation;

namespace SongsterGame.Api.Application.Features.JoinGame;

/// <summary>
/// Validator for JoinGameCommand.
/// </summary>
public sealed class JoinGameCommandValidator : AbstractValidator<JoinGameCommand>
{
    public JoinGameCommandValidator()
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

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .WithMessage("Nickname cannot be empty")
            .MinimumLength(2)
            .WithMessage("Nickname must be at least 2 characters")
            .MaximumLength(20)
            .WithMessage("Nickname cannot exceed 20 characters");
    }
}