#nullable enable
using Esolang.Processor;
using System.Diagnostics.CodeAnalysis;
using static Esolang.Processor.IOEvent;

namespace Esolang.Interpreter;

/// <summary>
/// Provides extension methods for running <see cref="IEventProcessor"/> in an interpreter context.
/// </summary>
[ExcludeFromCodeCoverage]
public static class InterpreterExtensions
{
    /// <summary>
    /// Executes the processor using standard I/O (Console.In, Console.Out).
    /// </summary>
    /// <param name="processor">The event processor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    public static async ValueTask<int> RunToConsoleAsync(
        this IEventProcessor processor,
        CancellationToken cancellationToken = default)
    {
        await foreach (var ioEvent in processor.RunAsyncEnumerable(cancellationToken))
        {
            switch (ioEvent)
            {
                case InputCharEvent charInput:
                    charInput.Write(await ReadCharFromConsoleAsync(cancellationToken));
                    break;
                case InputIntEvent intInput:
                    var line = await ReadLineFromConsoleAsync(cancellationToken);
                    if (int.TryParse(line, out var i))
                    {
                        intInput.Write(i);
                    } 
                    break;
                case OutputCharEvent charOutput:
                    Console.Write(charOutput.Output);
                    break;
                case OutputIntEvent intOutput:
                    Console.Write(intOutput.Output);
                    break;
                case EndEvent end:
                    return end.ExitCode;
            }
        }
        return 0;
    }

    static async ValueTask<char> ReadCharFromConsoleAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var c = Console.In.Read();
        return c == -1 ? '\0' : (char)c;
    }

    static async ValueTask<string?> ReadLineFromConsoleAsync(CancellationToken ct) => await Console.In.ReadLineAsync(ct);
}
