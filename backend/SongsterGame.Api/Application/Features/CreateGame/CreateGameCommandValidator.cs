using FluentValidation;

namespace SongsterGame.Api.Application.Features.CreateGame;

/// <summary>
/// Validator for CreateGameCommand.
/// </summary>
public sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required");

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .WithMessage("Nickname is required")
            .MinimumLength(2)
            .WithMessage("Nickname must be at least 2 characters")
            .MaximumLength(20)
            .WithMessage("Nickname cannot exceed 20 characters");
    }
}