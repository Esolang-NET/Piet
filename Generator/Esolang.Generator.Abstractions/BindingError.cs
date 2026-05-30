#nullable enable
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Esolang.Generator;

/// <summary>
/// Specifies the kind of diagnostic error that occurred during method signature binding.
/// </summary>
public enum BindingErrorKind
{
    /// <summary>The return type of the method is not supported.</summary>
    UnsupportedReturnType,
    /// <summary>A parameter has an invalid modifier (e.g., ref, out, in).</summary>
    InvalidParameterModifier,
    /// <summary>More than one parameter is competing for the same input role.</summary>
    DuplicateInput,
    /// <summary>More than one parameter is competing for the same output role.</summary>
    DuplicateOutput,
    /// <summary>More than one cancellation token parameter was found.</summary>
    DuplicateCancellationToken,
    /// <summary>More than one logger parameter was found.</summary>
    DuplicateLogger,
    /// <summary>A conflict exists between the return type and an output parameter.</summary>
    ReturnOutputConflict,
}

/// <summary>
/// Represents a diagnostic error that occurred during method signature binding.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract record BindingError
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Kind">The kind of error.</param>
    /// <param name="Location">The location associated with the error.</param>
    BindingError(BindingErrorKind Kind, Location? Location) : base()
     => (this.Kind, this.Location) = (Kind, Location);

    /// <summary>
    /// The kind of error.
    /// </summary>
    public BindingErrorKind Kind { get; }

    /// <summary>
    /// The location associated with the error.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// The return type of the method is not supported.
    /// </summary>
    /// <param name="ReturnType">The unsupported return type symbol.</param>
    /// <param name="Location">The location of the return type.</param>
    public sealed record UnsupportedReturnType(ITypeSymbol ReturnType, Location? Location)
        : BindingError(BindingErrorKind.UnsupportedReturnType, Location);

    /// <summary>
    /// A parameter has an invalid modifier (e.g., ref, out, in).
    /// </summary>
    /// <param name="Parameter">The parameter with the invalid modifier.</param>
    /// <param name="Location">The location of the parameter.</param>
    public sealed record InvalidParameterModifier(IParameterSymbol Parameter, Location? Location)
        : BindingError(BindingErrorKind.InvalidParameterModifier, Location);

    /// <summary>
    /// More than one parameter is competing for the same input role.
    /// </summary>
    /// <param name="Parameter">The parameter that caused the duplication.</param>
    /// <param name="ExistingKind">The kind of input that was already assigned.</param>
    /// <param name="Location">The location of the duplicate parameter.</param>
    public sealed record DuplicateInput(IParameterSymbol Parameter, MethodInputKind ExistingKind, Location? Location)
        : BindingError(BindingErrorKind.DuplicateInput, Location);

    /// <summary>
    /// More than one parameter is competing for the same output role.
    /// </summary>
    /// <param name="Parameter">The parameter that caused the duplication.</param>
    /// <param name="ExistingKind">The kind of output that was already assigned.</param>
    /// <param name="Location">The location of the duplicate parameter.</param>
    public sealed record DuplicateOutput(IParameterSymbol Parameter, MethodOutputKind ExistingKind, Location? Location)
        : BindingError(BindingErrorKind.DuplicateOutput, Location);

    /// <summary>
    /// More than one cancellation token parameter was found.
    /// </summary>
    /// <param name="Parameter">The duplicate cancellation token parameter.</param>
    /// <param name="Location">The location of the duplicate parameter.</param>
    public sealed record DuplicateCancellationToken(IParameterSymbol Parameter, Location? Location)
        : BindingError(BindingErrorKind.DuplicateCancellationToken, Location);

    /// <summary>
    /// More than one logger parameter was found.
    /// </summary>
    /// <param name="Parameter">The duplicate logger parameter.</param>
    /// <param name="Location">The location of the duplicate parameter.</param>
    public sealed record DuplicateLogger(IParameterSymbol Parameter, Location? Location)
        : BindingError(BindingErrorKind.DuplicateLogger, Location);

    /// <summary>
    /// A conflict exists between the return type and an output parameter.
    /// </summary>
    /// <param name="Parameter">The output parameter that conflicts with the return type.</param>
    /// <param name="Location">The location of the conflicting parameter.</param>
    public sealed record ReturnOutputConflict(IParameterSymbol Parameter, Location? Location)
        : BindingError(BindingErrorKind.ReturnOutputConflict, Location);

}
