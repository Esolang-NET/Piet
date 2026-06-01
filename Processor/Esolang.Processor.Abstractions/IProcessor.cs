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
