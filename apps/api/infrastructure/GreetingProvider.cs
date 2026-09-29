using Ida.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure;

public class GreetingProvider(IConfiguration config) : IGreetingProvider
{
    private readonly string _template =
        config["Greeting:Template"] ?? "Hello, {0}!";

    public string Greet(string name) => string.Format(_template, name);
}
