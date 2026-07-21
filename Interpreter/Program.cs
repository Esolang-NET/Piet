using Esolang.Piet.Interpreter;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

return await RunWithCancelRegisterAsync(args);

/// <summary>
/// Entry point for the dotnet-piet command-line tool.
/// </summary>
partial class Program
{
    /// <summary>
    /// Runs the dotnet-piet command-line tool with the specified arguments, input reader, and output writer.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="reader"></param>
    /// <param name="writer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    public static async Task<int> RunAsync(string[] args, TextReader? reader = null, TextWriter? writer = null, CancellationToken cancellationToken = default)
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
        try
        {
            return await RunAsync(args, cancellationToken);
        }
        finally
        {
            if (originalReader is not null)
                Console.SetIn(originalReader);
            if (originalWriter is not null)
                Console.SetOut(originalWriter);
        }
    }

    /// <summary>
    /// Runs the dotnet-piet command-line tool with the specified arguments and cancellation token.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
        => await new RootCommand()
            .AddPietCommands()
            .Parse(args)
            .InvokeAsync(cancellationToken: cancellationToken);

    /// <summary>
    /// Runs the dotnet-piet command-line tool with the specified arguments and registers a cancellation handler for Ctrl+C.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    static async Task<int> RunWithCancelRegisterAsync(string[] args)
    {
        try
        {
            Console.CancelKeyPress += CancelKeyPress;
            return await RunAsync(args, cancellationToken: cancellationTokenSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= CancelKeyPress;
        }
    }

    /// <summary>
    /// Cancellation token source used to signal cancellation when Ctrl+C is pressed.
    /// </summary>
    static readonly CancellationTokenSource cancellationTokenSource = new();

    /// <summary>
    /// Handles the Ctrl+C (CancelKeyPress) event by canceling the cancellation token source.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="e"></param>
    [ExcludeFromCodeCoverage]
    static void CancelKeyPress(object? _, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        cancellationTokenSource.Cancel();
    }
}
