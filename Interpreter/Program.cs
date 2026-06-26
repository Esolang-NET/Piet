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
    return await RunAsync(args, null, null, cts.Token);
}
finally
{
    Console.CancelKeyPress -= CancelKeyPress;
}
/// <summary>
/// Entry point for the dotnet-piet command-line tool.
/// </summary>
partial class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <param name="reader"></param>
    /// <param name="writer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> RunAsync(string[] args, TextReader? reader = null, TextWriter? writer = null,CancellationToken cancellationToken = default)
    {
        TextReader? originalReader = null;
        TextWriter? originalWriter = null;
        if (reader is not null)
        {
            originalReader = Console.In;
            Console.SetIn(reader);
        }
        if (writer is not null)
        {
            originalWriter = Console.Out;
            Console.SetOut(writer);
        }
        try {

            var rootCommand = PietInterpreterExtensions.BuildRootCommand();
            return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
        } finally
        {
            if (originalReader is not null)
                Console.SetIn(originalReader);
            if (originalWriter is not null)
                Console.SetOut(originalWriter);
        }
    }
}
