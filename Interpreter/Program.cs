using Esolang.Piet.Interpreter;
using System.CommandLine;

var rootCommand = PietInterpreterExtensions.BuildRootCommand();
return await rootCommand.Parse(args).InvokeAsync();

/// <summary>
/// Entry point for the dotnet-piet command-line tool.
/// </summary>
public partial class Program
{
    /// <inheritdoc/>
    public static async Task<int> RunAsync(string[] args)
    {
        var rootCommand = PietInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync();
    }
}
