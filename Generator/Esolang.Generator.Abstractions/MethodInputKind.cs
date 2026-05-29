namespace Esolang.Generator;
#nullable enable

/// <summary>
/// Specifies the input mechanism of the generated method.
/// </summary>
public enum MethodInputKind
{
    /// <summary>No explicit input mechanism.</summary>
    None,
    /// <summary>Input is provided via a string parameter.</summary>
    String,
    /// <summary>Input is provided via a TextReader parameter.</summary>
    TextReader,
    /// <summary>Input is provided via a PipeReader parameter.</summary>
    PipeReader,
}
