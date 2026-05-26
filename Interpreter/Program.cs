using Esolang.Piet.Interpreter;

CancellationTokenSource cts = new();
void CancelKeyPress(object? _, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cts.Cancel();
}
;

Console.CancelKeyPress += CancelKeyPress;
try
{
    return await RunAsync(args, cts.Token);
}
finally
{
    Console.CancelKeyPress -= CancelKeyPress;
}
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
