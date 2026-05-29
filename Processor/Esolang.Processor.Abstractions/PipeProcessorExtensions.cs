#nullable enable
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Esolang.Processor;

/// <summary>
/// Provides extension methods for running <see cref="IEventProcessor"/> using <see cref="PipeReader"/> and <see cref="PipeWriter"/>.
/// </summary>
public static class PipeProcessorExtensions
{
    /// <summary>
    /// Executes the processor until it reaches an <see cref="EndEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    /// <param name="input">The input pipe reader.</param>
    /// <param name="output">The output pipe writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input or output is null depending on the event.</exception>
    public static async ValueTask<int> RunToEndAsync(
        this IEventProcessor processor,
        PipeReader? input,
        PipeWriter? output,
        CancellationToken cancellationToken = default)
    {
        await foreach (var ev in processor.RunAsyncEnumerable(cancellationToken))
        {
            switch (ev)
            {
                case InputCharEvent inputChar:
                    if (input == null)
                        throw new ArgumentNullException(nameof(input));
                    var result = await input.ReadAtLeastAsync(1, cancellationToken);
                    var buffer = ArrayPool<byte>.Shared.Rent(1);
                    try
                    {
#if NETSTANDARD2_1_OR_GREATER
                        result.Buffer.Slice(0, 1).CopyTo(buffer.AsSpan());
#else
                        result.Buffer.Slice(0, 1).ToArray().CopyTo(buffer, 0);
#endif
                        input.AdvanceTo(result.Buffer.GetPosition(1));
                        inputChar.Write((char)buffer[0]);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                    break;
                case InputIntEvent inputInt:
                    if (input == null)
                        throw new ArgumentNullException(nameof(input));
                    var result2 = await input.ReadAtLeastAsync(1, cancellationToken);
                    var buffer2 = ArrayPool<byte>.Shared.Rent(1);
                    try
                    {
#if NETSTANDARD2_1_OR_GREATER
                        result2.Buffer.Slice(0, 4).CopyTo(buffer2.AsSpan());
#else
                        result2.Buffer.Slice(0, 4).ToArray().CopyTo(buffer2, 0);
#endif
                        input.AdvanceTo(result2.Buffer.GetPosition(4));
                        inputInt.Write(BitConverter.ToInt32(buffer2, 0));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer2);
                    }
                    break;
                case OutputCharEvent outputChar:
                    if (output == null)
                        throw new ArgumentNullException(nameof(output));
                    output.Write(Encoding.UTF8.GetBytes(new[] { outputChar.Output }));
                    await output.FlushAsync(cancellationToken);
                    break;
                case OutputIntEvent outputInt:
                    if (output == null)
                        throw new ArgumentNullException(nameof(output));
                    output.Write(Encoding.UTF8.GetBytes(outputInt.Output.ToString()));
                    await output.FlushAsync(cancellationToken);
                    break;
                case EndEvent end:
                    return end.ExitCode;
            }
        }
        return 0;
    }

    /// <summary>
    /// Executes the processor synchronously until it reaches an <see cref="EndEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    /// <param name="input">The input pipe reader.</param>
    /// <param name="output">The output pipe writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code.</returns>
    public static int RunToEnd(
        this IEventProcessor processor,
        PipeReader? input = null,
        PipeWriter? output = null,
        CancellationToken cancellationToken = default)
    {
        var result = RunToEndAsync(processor, input, output, cancellationToken);
        if (result.IsCompleted)
            return result.GetAwaiter().GetResult();
        return result.AsTask().GetAwaiter().GetResult();
    }
}
