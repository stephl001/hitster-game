using MediatR;

namespace SongsterGame.Api.Application.Common;

/// <summary>
/// Base class for synchronous MediatR request handlers.
/// Eliminates the need for Task.FromResult in handlers with synchronous logic.
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class SyncRequestHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : IRequest<TResponse>
{
    public Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken)
    {
        TResponse response = HandleCommand(request);
        return Task.FromResult(response);
    }

    /// <summary>
    /// Implement this method with synchronous logic.
    /// </summary>
    protected abstract TResponse HandleCommand(TCommand request);
}