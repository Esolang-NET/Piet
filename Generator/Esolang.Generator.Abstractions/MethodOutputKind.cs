namespace Esolang.Generator;

/// <summary>
/// Specifies the output mechanism of the generated method.
/// </summary>
public enum MethodOutputKind
{
    /// <summary>No explicit output mechanism.</summary>
    None,
    /// <summary>Output is written to a TextWriter parameter.</summary>
    TextWriter,
    /// <summary>Output is written to a PipeWriter parameter.</summary>
    PipeWriter,
    /// <summary>Output is returned as a string.</summary>
    ReturnString,
    /// <summary>Output is yielded via IEnumerable&lt;byte&gt;.</summary>
    ReturnIEnumerable,
    /// <summary>Output is yielded via IAsyncEnumerable&lt;byte&gt;.</summary>
    ReturnIAsyncEnumerable,
}
