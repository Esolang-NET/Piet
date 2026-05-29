#nullable enable
namespace Esolang.Processor;

/// <summary>
/// Common base interface for all processors.
/// </summary>
public interface IProcessor { }

/// <summary>
/// Common base interface for processors that hold a program to be executed.
/// </summary>
/// <typeparam name="TProgram">The type of the parsed program.</typeparam>
public interface IProcessor<TProgram> : IProcessor
{
    /// <summary>The parsed program.</summary>
    TProgram Program { get; }
}

/// <summary>
/// Execution model based on a stream of I/O events.
/// </summary>
public interface IEventProcessor : IProcessor
{
    /// <summary>
    /// Executes the processor and returns a stream of I/O events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of I/O events.</returns>
    IAsyncEnumerable<IOEvent> RunAsyncEnumerable(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an I/O event.
/// </summary>
public interface IOEvent
{

}

/// <summary>
/// Represents an event requesting a character input.
/// </summary>
public abstract class InputCharEvent : IOEvent
{
    /// <summary>
    /// Writes the input character to the processor.
    /// </summary>
    /// <param name="c">The input character.</param>
    public abstract void Write(char c);
}

/// <summary>
/// Represents an event requesting an integer input.
/// </summary>
public abstract class InputIntEvent : IOEvent
{
    /// <summary>
    /// Writes the input integer to the processor.
    /// </summary>
    /// <param name="i">The input integer.</param>
    public abstract void Write(int i);
}

/// <summary>
/// Represents an event that outputs a character.
/// </summary>
/// <param name="Output">The character to output.</param>
public sealed class OutputCharEvent(char Output) : IOEvent
{
    /// <summary>
    /// The character to output.
    /// </summary>
    public char Output { get; } = Output;
}

/// <summary>
/// Represents an event that outputs an integer.
/// </summary>
/// <param name="Output">The integer to output.</param>
public sealed class OutputIntEvent(int Output) : IOEvent
{
    /// <summary>
    /// The integer to output.
    /// </summary>
    public int Output { get; } = Output;
}

/// <summary>
/// Represents an event indicating the end of execution.
/// </summary>
/// <param name="exitCode">The exit code.</param>
public sealed class EndEvent(int exitCode) : IOEvent
{
    /// <summary>
    /// The exit code.
    /// </summary>
    public int ExitCode { get; } = exitCode;
}
