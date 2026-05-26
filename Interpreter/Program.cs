using Esolang.Piet.Interpreter;

CancellationTokenSource cts = new();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

return await RunAsync(args, cts.Token);

/// <summary>
/// Entry point for the dotnet-piet command-line tool.
/// </summary>
internal partial class Program
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var rootCommand = PietInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
    }
}
