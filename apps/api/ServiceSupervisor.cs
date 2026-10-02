using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ida.Start;

internal sealed class ServiceSupervisor(string apiDirectory, StartOptions options, IReadOnlyList<ServiceDefinition> services) : IAsyncDisposable
{
    private readonly List<(string Name, Process Process)> children = [];
    private readonly TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool stopping;
    public Task<int> Completion => completion.Task;

    private static readonly string[] InheritedVariables =
    [
        "PATH", "HOME", "USER", "LOGNAME", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "TEMP", "TMP", "TMPDIR",
        "SystemRoot", "WINDIR", "COMSPEC", "PATHEXT", "SystemDrive", "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432",
        "PROCESSOR_ARCHITECTURE", "NUMBER_OF_PROCESSORS", "LANG", "LC_ALL", "TERM", "TZ", "LD_LIBRARY_PATH",
        "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_ROOT_X86", "DOTNET_ROOT_ARM64", "DOTNET_CLI_HOME", "DOTNET_HOST_PATH",
        "DOTNET_CLI_TELEMETRY_OPTOUT", "DOTNET_NOLOGO", "DOTNET_USE_POLLING_FILE_WATCHER",
        "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "SSL_CERT_FILE", "SSL_CERT_DIR"
    ];

    public void ValidateProjects()
    {
        foreach (var service in services)
        {
            if (!File.Exists(Path.Combine(apiDirectory, service.ProjectPath)))
                throw new InvalidOperationException($"Project is missing for {service.Name}: {service.ProjectPath}.");
            if (options.Mode == "start" && !File.Exists(ReleaseAssembly(service)))
                throw new InvalidOperationException($"Build {service.Name} in Release before using start mode.");
        }
    }

    public void Start(CancellationToken cancellationToken)
    {
        foreach (var service in services)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (completion.Task.IsCompleted) throw new InvalidOperationException("A service exited while the system was starting.");
            var projectPath = Path.Combine(apiDirectory, service.ProjectPath);
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.Environment.Clear();
            foreach (var key in InheritedVariables)
                if (Environment.GetEnvironmentVariable(key) is { } value) start.Environment[key] = value;
            start.Environment["DOTNET_ENVIRONMENT"] = options.EnvironmentName;
            start.Environment["ASPNETCORE_ENVIRONMENT"] = options.EnvironmentName;
            start.Environment["IDA_START_SETTINGS"] = JsonSerializer.Serialize(service.Settings);
            start.Environment["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";
            start.Environment["DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH"] = "1";
            if (service.ListenUrl is not null) start.Environment["ASPNETCORE_URLS"] = service.ListenUrl;

            if (options.Mode == "start") start.ArgumentList.Add(ReleaseAssembly(service));
            else
            {
                var artifacts = Path.Combine(apiDirectory, "obj", "start", service.Name);
                if (options.Mode == "dev")
                    AddArguments(start, "watch", "--non-interactive", "--project", projectPath, "--artifacts-path", artifacts, "run");
                else
                    AddArguments(start, "run", "--project", projectPath, "--artifacts-path", artifacts);
                AddArguments(start, "--no-launch-profile", "--configuration", "Debug");
                if (service.IsWorker) start.ArgumentList.Add("--");
            }
            if (service.IsWorker) start.ArgumentList.Add("serve");

            var process = new Process { StartInfo = start };
            process.OutputDataReceived += (_, args) => WriteOutput(service.Name, args.Data);
            process.ErrorDataReceived += (_, args) => WriteOutput(service.Name, args.Data);
            Console.WriteLine($"Starting {service.Name} ({options.Mode})...");
            try
            {
                if (!process.Start()) throw new InvalidOperationException($"Could not start {service.Name}.");
                children.Add((service.Name, process));
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                _ = ObserveExitAsync(service.Name, process);
            }
            catch
            {
                if (!children.Any(child => child.Process == process)) process.Dispose();
                throw;
            }
        }
    }

    private async Task ObserveExitAsync(string name, Process process)
    {
        await process.WaitForExitAsync();
        if (stopping) return;
        Console.Error.WriteLine($"{name} exited unexpectedly (code {process.ExitCode}). Stopping this start-all session.");
        completion.TrySetResult(process.ExitCode == 0 ? 1 : process.ExitCode);
    }

    public async ValueTask DisposeAsync()
    {
        stopping = true;
        await Task.WhenAll(children.Select(async child =>
        {
            try
            {
                if (!child.Process.HasExited)
                {
                    Console.WriteLine($"Stopping {child.Name}...");
                    if (!OperatingSystem.IsWindows()) SendSignal(child.Process.Id, 2);
                    try { await child.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
                    catch (TimeoutException)
                    {
                        if (!child.Process.HasExited) child.Process.Kill(entireProcessTree: true);
                        await child.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or TimeoutException)
            {
                Console.Error.WriteLine($"Could not finish stopping {child.Name}: {exception.Message}");
            }
            finally { child.Process.Dispose(); }
        }));
        children.Clear();
    }

    private string ReleaseAssembly(ServiceDefinition service) => Path.Combine(apiDirectory, Path.GetDirectoryName(service.ProjectPath)!, "bin", "Release", "net9.0", service.AssemblyName + ".dll");

    private static void AddArguments(ProcessStartInfo start, params string[] arguments)
    {
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
    }

    private static void WriteOutput(string name, string? line)
    {
        if (line is not null) Console.WriteLine($"[{name}] {line}");
    }

    [DllImport("libc", EntryPoint = "kill")]
    private static extern int SendSignal(int processId, int signal);
}
