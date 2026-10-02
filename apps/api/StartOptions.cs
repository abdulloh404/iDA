namespace Ida.Start;

internal sealed record StartOptions(string Mode, string EnvironmentName)
{
    public static StartOptions Parse(string[] args)
    {
        var mode = "dev";
        var environment = "Local";
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index == 0 && argument is "dev" or "serve" or "start")
            {
                mode = argument;
                continue;
            }
            if (argument is not ("--mode" or "--environment") || index + 1 == args.Length)
                throw new InvalidOperationException("Usage: start-api [dev|serve|start] [--environment Local|Development|Production]");
            var value = args[++index];
            if (argument == "--mode") mode = value.ToLowerInvariant();
            else environment = value;
        }
        if (mode is not ("dev" or "serve" or "start")) throw new InvalidOperationException("Mode must be dev (watch), serve (run), or start (Release binaries).");
        environment = environment.ToLowerInvariant() switch
        {
            "local" => "Local",
            "development" => "Development",
            "production" => "Production",
            _ => throw new InvalidOperationException("Environment must be Local, Development, or Production.")
        };
        return new StartOptions(mode, environment);
    }
}
