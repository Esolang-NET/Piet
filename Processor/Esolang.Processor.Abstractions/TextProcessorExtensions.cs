#nullable enable
using System.Buffers;

namespace Esolang.Processor;

/// <summary>
/// Provides extension methods for running <see cref="IEventProcessor"/> using <see cref="TextReader"/> and <see cref="TextWriter"/>.
/// </summary>
public static class TextProcessorExtensions
{
    /// <summary>
    /// Executes the processor until it reaches an <see cref="EndEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    /// <param name="input">The input text reader.</param>
    /// <param name="output">The output text writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input or output is null depending on the event.</exception>
    public static async ValueTask<int> RunToEndAsync(
        this IEventProcessor processor,
        TextReader? input = null,
        TextWriter? output = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (var ioEvent in processor.RunAsyncEnumerable(cancellationToken))
        {
            switch (ioEvent)
            {
                case InputCharEvent charInput:
                    if (input is null)
                        throw new ArgumentNullException(nameof(input));
                    {
                        var buffer = ArrayPool<char>.Shared.Rent(1);
                        try
                        {
                            int read;
                            do
                            {
#if NETSTANDARD2_1_OR_GREATER
                                read = await input.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
#else
                                read = await input.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
#endif
                                if (read < 0) continue;
                                charInput.Write(buffer[0]);
                                break;
                            } while (read < 0 && !cancellationToken.IsCancellationRequested);
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(buffer);
                        }
                    }
                    break;
                case InputIntEvent intInput:
                    if (input is null)
                        throw new ArgumentNullException(nameof(input));
                    {
                        var inputString = await input.ReadLineAsync();
                        if (int.TryParse(inputString, out var i))
                        {
                            intInput.Write(i);
                        }
                    }
                    break;
                case OutputCharEvent charOutput:
                    if (output is null)
                        throw new ArgumentNullException(nameof(output));
                    {
                        await output.WriteAsync(charOutput.Output).ConfigureAwait(false);
                        await output.FlushAsync().ConfigureAwait(false);
                    }
                    break;
                case OutputIntEvent intOutput:
                    if (output is null)
                        throw new ArgumentNullException(nameof(output));
                    {
                        await output.WriteLineAsync(intOutput.Output.ToString()).ConfigureAwait(false);
                        await output.FlushAsync().ConfigureAwait(false);
                    }
                    break;
                case EndEvent endEvent:
                    return endEvent.ExitCode;
            }
        }
        return 0;
    }

    /// <summary>
    /// Executes the processor synchronously until it reaches an <see cref="EndEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    /// <param name="input">The input text reader.</param>
    /// <param name="output">The output text writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    [Obsolete("Use RunToEndAsync instead.")]
    public static int RunToEnd(
        this IEventProcessor processor,
        TextReader? input = null,
        TextWriter? output = null,
        CancellationToken cancellationToken = default)
    {
        var result = RunToEndAsync(processor, input, output, cancellationToken);
        if (result.IsCompleted)
            return result.GetAwaiter().GetResult();
        return result.AsTask().GetAwaiter().GetResult();
    }
}
