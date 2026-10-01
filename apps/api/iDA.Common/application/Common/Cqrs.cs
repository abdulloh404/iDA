using MediatR;
using Microsoft.Extensions.Logging;

namespace Ida.Application.Common;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>;

public class CqrsLoggingBehavior<TRequest, TResponse>(
    ILogger<CqrsLoggingBehavior<TRequest, TResponse>> log)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var kind = request is ICommand<TResponse> ? "CMD"
            : request is IQuery<TResponse> ? "QRY" : "REQ";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        log.LogInformation("[{Kind}] {Request} handled in {Ms} ms",
            kind, typeof(TRequest).Name, sw.ElapsedMilliseconds);
        return response;
    }
}

