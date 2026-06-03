using Esolang.Piet.Parser;
using Esolang.Processor;

namespace Esolang.Piet.Processor;

/// <summary>
/// Executes parsed Piet programs.
/// </summary>
/// <remarks>
/// Initializes the processor with a parsed Piet program.
/// </remarks>
public sealed partial class PietProcessor : IProcessor<PietProgram>
{
    PietProgram IProcessor<PietProgram>.Program => program;
}
