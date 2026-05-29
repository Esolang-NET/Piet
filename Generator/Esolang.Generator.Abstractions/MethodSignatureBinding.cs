#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Esolang.Generator;

/// <summary>
/// Represents the result of binding a method signature for generation.
/// </summary>
/// <param name="IsValid">Whether the binding is successful.</param>
/// <param name="ReturnKind">The return kind of the method.</param>
/// <param name="InputKind">The input kind of the method.</param>
/// <param name="OutputKind">The output kind of the method.</param>
/// <param name="InputExpression">The expression to access the input (e.g., parameter name).</param>
/// <param name="OutputExpression">The expression to access the output (e.g., parameter name).</param>
/// <param name="CancellationTokenName">The name of the cancellation token parameter, if any.</param>
/// <param name="LoggerExpression">The expression to access the logger (e.g., "loggerParam", "this._logger").</param>
/// <param name="IsLoggerFromParameter">Whether the logger is obtained from a method parameter.</param>
/// <param name="UnhandledParameters">Parameters that were not handled by the common binding logic.</param>
/// <param name="ErrorId">The diagnostic error ID if the binding failed.</param>
/// <param name="Location">The location associated with the error.</param>
[DebuggerDisplay("{ToString(),nq}")]
[ExcludeFromCodeCoverage]
public record struct MethodSignatureBinding(
    bool IsValid,
    MethodReturnKind ReturnKind,
    MethodInputKind InputKind,
    MethodOutputKind OutputKind,
    string InputExpression,
    string OutputExpression,
    string? CancellationTokenName,
    string? LoggerExpression,
    bool IsLoggerFromParameter,
    IReadOnlyList<IParameterSymbol> UnhandledParameters,
    string? ErrorId = null,
    Location? Location = null)
{
    /// <summary>Gets a value indicating whether the method has an explicit input mechanism.</summary>
    public readonly bool HasExplicitInput => InputKind != MethodInputKind.None;

    /// <summary>Gets a value indicating whether the method has an explicit output mechanism.</summary>
    public readonly bool HasExplicitOutput => OutputKind != MethodOutputKind.None;

    /// <summary>Gets a value indicating whether the method is asynchronous.</summary>
    public readonly bool IsAsync => ReturnKind switch
    {
        MethodReturnKind.Task or MethodReturnKind.TaskInt32 or MethodReturnKind.TaskString or MethodReturnKind.TaskNullableString or
        MethodReturnKind.ValueTask or MethodReturnKind.ValueTaskInt32 or MethodReturnKind.ValueTaskString or MethodReturnKind.ValueTaskNullableString or
        MethodReturnKind.IAsyncEnumerableByte => true,
        _ => false
    };

    /// <summary>Gets a value indicating whether the method returns an enumerable.</summary>
    public readonly bool IsEnumerable => ReturnKind == MethodReturnKind.IEnumerableByte;

    /// <summary>Gets a value indicating whether the method returns an async enumerable.</summary>
    public readonly bool IsAsyncEnumerable => ReturnKind == MethodReturnKind.IAsyncEnumerableByte;

    [ExcludeFromCodeCoverage]
    readonly bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(IsValid)).Append('=').Append(IsValid).Append(", ");
        builder.Append(nameof(ReturnKind)).Append('=').Append(ReturnKind).Append(", ");
        builder.Append(nameof(InputKind)).Append('=').Append(InputKind).Append(", ");
        builder.Append(nameof(OutputKind)).Append('=').Append(OutputKind).Append(", ");
        builder.Append(nameof(InputExpression)).Append('=').Append(InputExpression).Append(", ");
        builder.Append(nameof(OutputExpression)).Append('=').Append(OutputExpression).Append(", ");
        builder.Append(nameof(CancellationTokenName)).Append('=').Append(CancellationTokenName).Append(", ");
        builder.Append(nameof(LoggerExpression)).Append('=').Append(LoggerExpression).Append(", ");
        builder.Append(nameof(IsLoggerFromParameter)).Append('=').Append(IsLoggerFromParameter).Append(", ");
        builder.Append(nameof(UnhandledParameters)).Append("=[");
        for (var i = 0; i < UnhandledParameters.Count; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append(UnhandledParameters[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
        builder.Append(']');
        return true;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override readonly string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(nameof(MethodSignatureBinding)).Append(" {");
        if (!PrintMembers(builder))
        {
            builder.Append(' ');
        }
        builder.Append('}');
        return builder.ToString();
    }

}
