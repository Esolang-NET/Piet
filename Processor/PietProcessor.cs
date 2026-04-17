using Esolang.Piet.Parser;

namespace Esolang.Piet.Processor;

/// <summary>
/// Minimal placeholder processor for Piet execution.
/// </summary>
public sealed class PietProcessor
{
    /// <summary>
    /// Initializes the processor with a parsed Piet program.
    /// </summary>
    public PietProcessor(PietProgram program)
    {
        Program = program;
    }

    /// <summary>
    /// The parsed Piet program.
    /// </summary>
    public PietProgram Program { get; }

    /// <summary>
    /// Executes the program.
    /// </summary>
    public void Run()
    {
        throw new NotImplementedException("Piet execution is not implemented yet.");
    }
}
