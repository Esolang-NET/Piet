using Esolang.Piet.Interpreter;

namespace Esolang.Piet.Interpreter;

/// <summary>
/// Entry point for the dotnet-piet command-line tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the command-line pipeline and returns the process exit code.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var rootCommand = PietInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>Application entry point.</summary>
    public static async Task<int> Main(string[] args)
        => await RunAsync(args);
}
