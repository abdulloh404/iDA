using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ida.Start;

internal sealed record ApiService(string Id, string Name, string Directory, string Project, string Assembly, string[] Arguments, IReadOnlyDictionary<string, string> Environment);

internal sealed class ServiceSupervisor(string apiRoot, string mode, IReadOnlyList<ApiService> services, IHostApplicationLifetime lifetime, ILogger<ServiceSupervisor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var children = new List<(ApiService Service, Process Process)>();
        var outputTasks = new List<Task>();
        using var outputCancellation = new CancellationTokenSource();
        ApiService? currentService = null;
        try
        {
            var applicationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(() => applicationStarted.TrySetResult());
            await applicationStarted.Task.WaitAsync(stoppingToken);

            foreach (var service in services)
            {
                stoppingToken.ThrowIfCancellationRequested();
                currentService = service;
                var process = new Process { StartInfo = CreateStartInfo(service) };
                var started = false;
                try
                {
                    logger.LogInformation("Starting {ServiceName} ({Mode}).", service.Name, mode);
                    started = process.Start();
                    if (!started) throw new InvalidOperationException($"Unable to start {service.Name}.");
                    children.Add((service, process));
                    process.StandardInput.Close();
                    outputTasks.Add(ReadOutputAsync(process.StandardOutput, service.Name, false, outputCancellation.Token));
                    outputTasks.Add(ReadOutputAsync(process.StandardError, service.Name, true, outputCancellation.Token));
                }
                catch
                {
                    if (!started) process.Dispose();
                    throw;
                }
            }

            currentService = null;
            if (children.Count == 0)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                return;
            }

            var exits = children.Select(child => child.Process.WaitForExitAsync()).ToArray();
            var completed = await Task.WhenAny(exits).WaitAsync(stoppingToken);
            await completed;
            if (!stoppingToken.IsCancellationRequested)
            {
                var child = children[Array.IndexOf(exits, completed)];
                currentService = child.Service;
                var exitCode = child.Process.ExitCode;
                Environment.ExitCode = exitCode == 0 ? 1 : exitCode;
                logger.LogError("{ServiceName} exited unexpectedly with code {ExitCode}.", child.Service.Name, exitCode);
                lifetime.StopApplication();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 1;
            logger.LogError(exception, "API service {ServiceName} failed.", currentService?.Name ?? "supervisor");
            lifetime.StopApplication();
        }
        finally
        {
            await StopChildrenAsync(children);
            outputCancellation.Cancel();
            try
            {
                await Task.WhenAll(outputTasks).WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Timed out reading final API service output.");
            }
            finally
            {
                foreach (var child in children) child.Process.Dispose();
            }
        }
    }

    private ProcessStartInfo CreateStartInfo(ApiService service)
    {
        var directory = Path.Combine(apiRoot, service.Directory);
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (mode == "start")
        {
            var assembly = Path.Combine(directory, service.Assembly);
            if (!File.Exists(assembly)) assembly = Path.Combine(directory, "bin", "Release", "net9.0", service.Assembly);
            startInfo.ArgumentList.Add(assembly);
        }
        else
        {
            if (mode == "dev")
            {
                startInfo.ArgumentList.Add("watch");
                startInfo.ArgumentList.Add("--non-interactive");
            }
            else if (mode == "serve") startInfo.ArgumentList.Add("run");
            else throw new ArgumentException($"Unsupported API mode '{mode}'.", nameof(mode));
            startInfo.ArgumentList.Add("--artifacts-path");
            startInfo.ArgumentList.Add(Path.Combine(apiRoot, "obj", "services", mode, service.Id));
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(service.Project);
            if (mode == "dev") startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--no-launch-profile");
            startInfo.ArgumentList.Add("--");
        }
        foreach (var argument in service.Arguments) startInfo.ArgumentList.Add(argument);
        foreach (var (key, value) in service.Environment)
        {
            foreach (var existing in startInfo.Environment.Keys.Where(existing => string.Equals(existing, key, StringComparison.OrdinalIgnoreCase)).ToArray()) startInfo.Environment.Remove(existing);
            startInfo.Environment[key] = value;
        }
        return startInfo;
    }

    private async Task ReadOutputAsync(StreamReader reader, string serviceName, bool error, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null) break;
                if (error) logger.LogWarning("[{ServiceName}] {Message}", serviceName, line);
                else logger.LogInformation("[{ServiceName}] {Message}", serviceName, line);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            if (!cancellationToken.IsCancellationRequested) logger.LogWarning(exception, "Unable to read output from {ServiceName}.", serviceName);
        }
    }

    private async Task StopChildrenAsync(IReadOnlyList<(ApiService Service, Process Process)> children)
    {
        if (children.Count == 0) return;
        var exits = children.Select(child => child.Process.WaitForExitAsync()).ToArray();
        try
        {
            await Task.WhenAll(exits).WaitAsync(TimeSpan.FromSeconds(3));
            return;
        }
        catch (TimeoutException)
        {
        }
        foreach (var child in children)
        {
            try
            {
                if (!child.Process.HasExited) child.Process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                logger.LogWarning(exception, "Unable to stop {ServiceName}.", child.Service.Name);
            }
        }
        try
        {
            await Task.WhenAll(exits).WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Timed out stopping API services.");
        }
    }
}
