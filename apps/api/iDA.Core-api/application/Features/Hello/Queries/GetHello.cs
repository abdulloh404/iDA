using Ida.Application.Common;

namespace Ida.Application.Features.Hello.Queries;

public record HelloDto(string Message, DateTimeOffset ServerTime);

public record GetHelloQuery(string? Name) : IQuery<HelloDto>;

public class GetHelloHandler(IGreetingProvider greetings)
    : IQueryHandler<GetHelloQuery, HelloDto>
{
    public Task<HelloDto> Handle(GetHelloQuery query, CancellationToken ct)
    {
        var name = string.IsNullOrWhiteSpace(query.Name) ? "World" : query.Name.Trim();
        if (name.Length > 64)
            throw ApiException.BadRequest("name_too_long", "ชื่อยาวเกิน 64 ตัวอักษร");

        return Task.FromResult(new HelloDto(greetings.Greet(name), DateTimeOffset.Now));
    }
}

