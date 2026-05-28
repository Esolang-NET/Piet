using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Esolang.Generator;

/// <summary>
/// Holds resolved type symbols for a compilation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnownTypes"/> struct.
/// </remarks>
/// <param name="compilation">The compilation to resolve types from.</param>
public readonly struct KnownTypes(Compilation compilation)
{
    /// <summary>The <c>string</c> type symbol.</summary>
    public readonly INamedTypeSymbol? String = compilation.GetSpecialType(SpecialType.System_String);
    /// <summary>The <c>byte</c> type symbol.</summary>
    public readonly INamedTypeSymbol? Byte = compilation.GetSpecialType(SpecialType.System_Byte);
    /// <summary>The <c>int</c> type symbol.</summary>
    public readonly INamedTypeSymbol? Int32 = compilation.GetSpecialType(SpecialType.System_Int32);
    /// <summary>The <c>System.Threading.Tasks.Task</c> type symbol.</summary>
    public readonly INamedTypeSymbol? Task = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
    /// <summary>The <c>System.Threading.Tasks.Task{TResult}</c> type symbol.</summary>
    public readonly INamedTypeSymbol? TaskT = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
    /// <summary>The <c>System.Threading.Tasks.ValueTask</c> type symbol.</summary>
    public readonly INamedTypeSymbol? ValueTask = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
    /// <summary>The <c>System.Threading.Tasks.ValueTask{TResult}</c> type symbol.</summary>
    public readonly INamedTypeSymbol? ValueTaskT = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    /// <summary>The <c>System.Collections.Generic.IEnumerable{T}</c> type symbol.</summary>
    public readonly INamedTypeSymbol? IEnumerableT = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
    /// <summary>The <c>System.Collections.Generic.IAsyncEnumerable{T}</c> type symbol.</summary>
    public readonly INamedTypeSymbol? IAsyncEnumerableT = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
    /// <summary>The <c>System.IO.Pipelines.PipeReader</c> type symbol.</summary>
    public readonly INamedTypeSymbol? PipeReader = compilation.GetBestTypeByMetadataName("System.IO.Pipelines.PipeReader");
    /// <summary>The <c>System.IO.Pipelines.PipeWriter</c> type symbol.</summary>
    public readonly INamedTypeSymbol? PipeWriter = compilation.GetBestTypeByMetadataName("System.IO.Pipelines.PipeWriter");
    /// <summary>The <c>System.IO.TextReader</c> type symbol.</summary>
    public readonly INamedTypeSymbol? TextReader = compilation.GetBestTypeByMetadataName("System.IO.TextReader");
    /// <summary>The <c>System.IO.TextWriter</c> type symbol.</summary>
    public readonly INamedTypeSymbol? TextWriter = compilation.GetBestTypeByMetadataName("System.IO.TextWriter");
    /// <summary>The <c>System.Threading.CancellationToken</c> type symbol.</summary>
    public readonly INamedTypeSymbol? CancellationToken = compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken");
    /// <summary>The <c>Microsoft.Extensions.Logging.ILogger</c> type symbol.</summary>
    public readonly INamedTypeSymbol? ILogger = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
    /// <summary>The <c>Microsoft.Extensions.Logging.ILogger{T}</c> type symbol.</summary>
    public readonly INamedTypeSymbol? ILoggerT = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1");

    private static bool EqualsDefinition(ITypeSymbol? type, ISymbol? symbol) =>
        type != null && symbol != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, symbol);

    private static bool EqualsType(ITypeSymbol? type, ISymbol? symbol) =>
        type != null && symbol != null && SymbolEqualityComparer.Default.Equals(type, symbol);

    /// <summary>Gets a value indicating whether the type is <c>string</c>.</summary>
    /// <param name="type">The type to check.</param>
    /// <param name="isNullable">Optional: Whether to check for nullability.</param>
    public readonly bool IsString(ITypeSymbol? type, bool? isNullable = null)
    {
        if (type is not INamedTypeSymbol named || !SymbolEqualityComparer.Default.Equals(named, String)) return false;
        if (isNullable == null) return true;
        if (isNullable.Value) return type.NullableAnnotation == NullableAnnotation.Annotated;
        return type.NullableAnnotation is NullableAnnotation.NotAnnotated or NullableAnnotation.None;
    }

    /// <summary>Gets a value indicating whether the type is <c>byte</c>.</summary>
    public readonly bool IsByte(ITypeSymbol? type) => EqualsType(type, Byte);
    /// <summary>Gets a value indicating whether the type is <c>int</c>.</summary>
    public readonly bool IsInt32(ITypeSymbol? type) => EqualsType(type, Int32);

    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.Task</c>.</summary>
    public readonly bool IsTask(ITypeSymbol? type) => EqualsType(type, Task);
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.Task{TResult}</c>.</summary>
    public readonly bool IsTaskT(ITypeSymbol? type, bool? isNullable = null)
    {
        if (type is not INamedTypeSymbol named || !EqualsDefinition(named, TaskT)) return false;
        if (isNullable == null) return true;
        var annotation = named.TypeArguments[0].NullableAnnotation;
        return isNullable.Value ? annotation == NullableAnnotation.Annotated : annotation is NullableAnnotation.NotAnnotated or NullableAnnotation.None;
    }
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.ValueTask</c>.</summary>
    public readonly bool IsValueTask(ITypeSymbol? type) => EqualsType(type, ValueTask);
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.ValueTask{TResult}</c>.</summary>
    public readonly bool IsValueTaskT(ITypeSymbol? type, bool? isNullable = null)
    {
        if (type is not INamedTypeSymbol named || !EqualsDefinition(named, ValueTaskT)) return false;
        if (isNullable == null) return true;
        var annotation = named.TypeArguments[0].NullableAnnotation;
        return isNullable.Value ? annotation == NullableAnnotation.Annotated : annotation is NullableAnnotation.NotAnnotated or NullableAnnotation.None;
    }
    /// <summary>Gets a value indicating whether the type is <c>System.Collections.Generic.IEnumerable{T}</c>.</summary>
    public readonly bool IsIEnumerableT(ITypeSymbol? type) => EqualsDefinition(type, IEnumerableT);
    /// <summary>Gets a value indicating whether the type is <c>System.Collections.Generic.IAsyncEnumerable{T}</c>.</summary>
    public readonly bool IsIAsyncEnumerableT(ITypeSymbol? type) => EqualsDefinition(type, IAsyncEnumerableT);

    /// <summary>Gets a value indicating whether the type is <c>System.IO.Pipelines.PipeReader</c>.</summary>
    public readonly bool IsPipeReader(ITypeSymbol? type) => EqualsType(type, PipeReader);
    /// <summary>Gets a value indicating whether the type is <c>System.IO.Pipelines.PipeWriter</c>.</summary>
    public readonly bool IsPipeWriter(ITypeSymbol? type) => EqualsType(type, PipeWriter);
    /// <summary>Gets a value indicating whether the type is <c>System.IO.TextReader</c>.</summary>
    public readonly bool IsTextReader(ITypeSymbol? type) => EqualsType(type, TextReader);
    /// <summary>Gets a value indicating whether the type is <c>System.IO.TextWriter</c>.</summary>
    public readonly bool IsTextWriter(ITypeSymbol? type) => EqualsType(type, TextWriter);
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.CancellationToken</c>.</summary>
    public readonly bool IsCancellationToken(ITypeSymbol? type) => EqualsType(type, CancellationToken);

    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.Task{String}</c>.</summary>
    public readonly bool IsTaskString(ITypeSymbol? type, bool? isNullable = null) => IsTaskT(type, isNullable) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_String;
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.ValueTask{String}</c>.</summary>
    public readonly bool IsValueTaskString(ITypeSymbol? type, bool? isNullable = null) => IsValueTaskT(type, isNullable) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_String;

    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.Task{Int32}</c>.</summary>
    public readonly bool IsTaskInt32(ITypeSymbol? type) => IsTaskT(type) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_Int32;
    /// <summary>Gets a value indicating whether the type is <c>System.Threading.Tasks.ValueTask{Int32}</c>.</summary>
    public readonly bool IsValueTaskInt32(ITypeSymbol? type) => IsValueTaskT(type) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_Int32;

    /// <summary>Gets a value indicating whether the type is <c>System.Collections.Generic.IEnumerable{Byte}</c>.</summary>
    public readonly bool IsIEnumerableByte(ITypeSymbol? type) => IsIEnumerableT(type) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_Byte;
    /// <summary>Gets a value indicating whether the type is <c>System.Collections.Generic.IAsyncEnumerable{Byte}</c>.</summary>
    public readonly bool IsIAsyncEnumerableByte(ITypeSymbol? type) => IsIAsyncEnumerableT(type) && ((INamedTypeSymbol)type!).TypeArguments[0].SpecialType == SpecialType.System_Byte;

    /// <summary>Gets a value indicating whether the type is a logger type (<c>ILogger</c> or <c>ILogger{T}</c>).</summary>
    public readonly bool IsLogger(ITypeSymbol? type)
    {
        if (type == null) return false;
        if (EqualsType(type, ILogger) || EqualsDefinition(type, ILoggerT)) return true;
        foreach (var iface in type.AllInterfaces)
        {
            if (EqualsType(iface, ILogger) || EqualsDefinition(iface, ILoggerT)) return true;
        }
        return false;
    }

    [ExcludeFromCodeCoverage]
    readonly bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(String)).Append('=');
        AppendNamedTypeSymbol(String, builder);
        builder.Append(", ");

        builder.Append(nameof(Byte)).Append('=');
        AppendNamedTypeSymbol(Byte, builder);
        builder.Append(", ");

        builder.Append(nameof(Int32)).Append('=');
        AppendNamedTypeSymbol(Int32, builder);
        builder.Append(", ");

        builder.Append(nameof(Task)).Append('=');
        AppendNamedTypeSymbol(Task, builder);
        builder.Append(", ");

        builder.Append(nameof(TaskT)).Append('=');
        AppendNamedTypeSymbol(TaskT, builder);
        builder.Append(", ");

        builder.Append(nameof(ValueTask)).Append('=');
        AppendNamedTypeSymbol(ValueTask, builder);
        builder.Append(", ");

        builder.Append(nameof(ValueTaskT)).Append('=');
        AppendNamedTypeSymbol(ValueTaskT, builder);
        builder.Append(", ");

        builder.Append(nameof(IEnumerableT)).Append('=');
        AppendNamedTypeSymbol(IEnumerableT, builder);
        builder.Append(", ");

        builder.Append(nameof(IAsyncEnumerableT)).Append('=');
        AppendNamedTypeSymbol(IAsyncEnumerableT, builder);
        builder.Append(", ");

        builder.Append(nameof(PipeReader)).Append('=');
        AppendNamedTypeSymbol(PipeReader, builder);
        builder.Append(", ");

        builder.Append(nameof(PipeWriter)).Append('=');
        AppendNamedTypeSymbol(PipeWriter, builder);
        builder.Append(", ");

        builder.Append(nameof(TextReader)).Append('=');
        AppendNamedTypeSymbol(TextReader, builder);
        builder.Append(", ");

        builder.Append(nameof(TextWriter)).Append('=');
        AppendNamedTypeSymbol(TextWriter, builder);
        builder.Append(", ");

        builder.Append(nameof(CancellationToken)).Append('=');
        AppendNamedTypeSymbol(CancellationToken, builder);
        builder.Append(", ");

        builder.Append(nameof(ILogger)).Append('=');
        AppendNamedTypeSymbol(ILogger, builder);
        builder.Append(", ");

        builder.Append(nameof(ILoggerT)).Append('=');
        AppendNamedTypeSymbol(ILoggerT, builder);
        
        return true;
        static void AppendNamedTypeSymbol(INamedTypeSymbol? symbol, StringBuilder builder)
        {
            if (symbol == null) return;
            builder.Append('(');
            builder.Append(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            builder.Append(", ");
            builder.Append(nameof(symbol.NullableAnnotation)).Append('=').Append(symbol.NullableAnnotation);
            builder.Append(')');
        }
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(nameof(KnownTypes)).Append(" {");
        if (!PrintMembers(builder))
        {
            builder.Append(' ');
        }
        builder.Append('}');
        return builder.ToString();
    }
}

/// <summary>
/// Provides utility methods for resolving types from a <see cref="Compilation"/>.
/// </summary>
public static class TypeResolutionExtensions
{
    /// <summary>
    /// Resolves the best <see cref="INamedTypeSymbol"/> for the specified metadata name.
    /// </summary>
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);
        if (type != null) return type;

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            var found = assembly.GetTypeByMetadataName(metadataName);
            if (found != null) return found;
        }
        return null;
    }
}
