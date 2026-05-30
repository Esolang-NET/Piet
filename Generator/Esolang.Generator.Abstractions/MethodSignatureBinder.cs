#nullable enable
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using static Esolang.Generator.BindingError;

namespace Esolang.Generator;

/// <summary>
/// Provides utility methods for binding method signatures to <see cref="MethodSignatureBinding"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public static class MethodSignatureBinder
{
    /// <summary>
    /// Binds the specified method symbol to a <see cref="MethodSignatureBinding"/>.
    /// </summary>
    /// <param name="method">The method symbol to bind.</param>
    /// <param name="types">The known types for the compilation.</param>
    /// <returns>The result of the binding.</returns>
    public static MethodSignatureBinding Bind(
        IMethodSymbol method,
        KnownTypes types)
    {
        var returnKind = BindReturnKind(method.ReturnType, types);
        if (returnKind == MethodReturnKind.Invalid)
        {
            return new MethodSignatureBinding(returnKind, MethodInputKind.None, MethodOutputKind.None, "", "", null, null, false, method.Parameters, new UnsupportedReturnType(method.ReturnType, method.Locations.FirstOrDefault()));
        }

        var outputKind = BindDefaultOutputKind(returnKind);
        var inputKind = MethodInputKind.None;
        var inputExpr = "";
        var outputExpr = "";
        string? cancellationTokenName = null;
        string? loggerExpression = null;
        var isLoggerFromParameter = false;
        var unhandledParameters = new List<IParameterSymbol>();

        foreach (var p in method.Parameters)
        {
            if (p.RefKind != RefKind.None)
            {
                return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new InvalidParameterModifier(p, p.Locations.FirstOrDefault()));
            }

            if (types.IsString(p.Type, false))
            {
                if (inputKind != MethodInputKind.None)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateInput(p, inputKind, p.Locations.FirstOrDefault()));

                inputKind = MethodInputKind.String;
                inputExpr = p.Name;
                continue;
            }

            if (types.IsTextReader(p.Type))
            {
                if (inputKind != MethodInputKind.None)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateInput(p, inputKind, p.Locations.FirstOrDefault()));

                inputKind = MethodInputKind.TextReader;
                inputExpr = p.Name;
                continue;
            }

            if (types.IsPipeReader(p.Type))
            {
                if (inputKind != MethodInputKind.None)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateInput(p, inputKind, p.Locations.FirstOrDefault()));

                inputKind = MethodInputKind.PipeReader;
                inputExpr = p.Name;
                continue;
            }

            if (types.IsTextWriter(p.Type))
            {
                if (IsOutputReturning(returnKind))
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new ReturnOutputConflict(p, p.Locations.FirstOrDefault()));

                if (outputKind != MethodOutputKind.None)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateOutput(p, outputKind, p.Locations.FirstOrDefault()));

                outputKind = MethodOutputKind.TextWriter;
                outputExpr = p.Name;
                continue;
            }

            if (types.IsPipeWriter(p.Type))
            {
                if (IsOutputReturning(returnKind))
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new ReturnOutputConflict(p, p.Locations.FirstOrDefault()));

                if (outputKind != MethodOutputKind.None)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateOutput(p, outputKind, p.Locations.FirstOrDefault()));

                outputKind = MethodOutputKind.PipeWriter;
                outputExpr = p.Name;
                continue;
            }

            if (types.IsCancellationToken(p.Type))
            {
                if (cancellationTokenName != null)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateCancellationToken(p, p.Locations.FirstOrDefault()));

                cancellationTokenName = p.Name;
                continue;
            }

            if (types.IsLogger(p.Type))
            {
                if (loggerExpression != null)
                    return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, method.Parameters, new DuplicateLogger(p, p.Locations.FirstOrDefault()));

                loggerExpression = p.Name;
                isLoggerFromParameter = true;
                continue;
            }

            unhandledParameters.Add(p);
        }

        loggerExpression ??= FindLoggerInContainingType(method.ContainingType, method.IsStatic, types, out isLoggerFromParameter);

        return new MethodSignatureBinding(returnKind, inputKind, outputKind, inputExpr, outputExpr, cancellationTokenName, loggerExpression, isLoggerFromParameter, unhandledParameters);
    }

    /// <summary>
    /// Binds the return type symbol to a <see cref="MethodReturnKind"/>.
    /// </summary>
    public static MethodReturnKind BindReturnKind(ITypeSymbol returnType, KnownTypes types)
    {
        if (returnType.SpecialType == SpecialType.System_Void) return MethodReturnKind.Void;
        if (returnType.SpecialType == SpecialType.System_Int32) return MethodReturnKind.Int32;
        if (types.IsString(returnType, false)) return MethodReturnKind.String;
        if (types.IsString(returnType, true)) return MethodReturnKind.NullableString;
        if (types.IsTask(returnType)) return MethodReturnKind.Task;
        if (types.IsTaskInt32(returnType)) return MethodReturnKind.TaskInt32;
        if (types.IsTaskString(returnType, false)) return MethodReturnKind.TaskString;
        if (types.IsTaskString(returnType, true)) return MethodReturnKind.TaskNullableString;
        if (types.IsValueTask(returnType)) return MethodReturnKind.ValueTask;
        if (types.IsValueTaskInt32(returnType)) return MethodReturnKind.ValueTaskInt32;
        if (types.IsValueTaskString(returnType, false)) return MethodReturnKind.ValueTaskString;
        if (types.IsValueTaskString(returnType, true)) return MethodReturnKind.ValueTaskNullableString;
        if (types.IsIEnumerableByte(returnType)) return MethodReturnKind.IEnumerableByte;
        if (types.IsIAsyncEnumerableByte(returnType)) return MethodReturnKind.IAsyncEnumerableByte;

        return MethodReturnKind.Invalid;
    }

    /// <summary>
    /// Gets the default output kind based on the return kind.
    /// </summary>
    static MethodOutputKind BindDefaultOutputKind(MethodReturnKind returnKind) => returnKind switch
    {
        MethodReturnKind.String or MethodReturnKind.NullableString or MethodReturnKind.TaskString or MethodReturnKind.TaskNullableString or MethodReturnKind.ValueTaskString or MethodReturnKind.ValueTaskNullableString => MethodOutputKind.ReturnString,
        MethodReturnKind.IEnumerableByte => MethodOutputKind.ReturnIEnumerable,
        MethodReturnKind.IAsyncEnumerableByte => MethodOutputKind.ReturnIAsyncEnumerable,
        _ => MethodOutputKind.None
    };

    /// <summary>
    /// Gets a value indicating whether the return kind implies output is returned.
    /// </summary>
    static bool IsOutputReturning(MethodReturnKind returnKind) => returnKind switch
    {
        MethodReturnKind.String or MethodReturnKind.NullableString or MethodReturnKind.TaskString or MethodReturnKind.TaskNullableString or MethodReturnKind.ValueTaskString or MethodReturnKind.ValueTaskNullableString or MethodReturnKind.IEnumerableByte or MethodReturnKind.IAsyncEnumerableByte => true,
        _ => false
    };

    /// <summary>
    /// Searches for a logger in the containing type (fields or constructor parameters).
    /// </summary>
    /// <param name="type">The type to search in.</param>
    /// <param name="isStatic">Whether the target method is static.</param>
    /// <param name="types">The known types for the compilation.</param>
    /// <param name="isFromParameter">Output: Whether the logger was found in a constructor parameter.</param>
    /// <returns>The expression to access the logger, or <c>null</c> if not found.</returns>
    static string? FindLoggerInContainingType(ITypeSymbol? type, bool isStatic, KnownTypes types, out bool isFromParameter)
    {
        isFromParameter = false;
        var currentType = type;
        var shadowedNames = new HashSet<string>(StringComparer.Ordinal);
        var isBaseType = false;

        while (currentType != null)
        {
            foreach (var field in currentType.GetMembers().OfType<IFieldSymbol>())
            {
                if (isStatic && !field.IsStatic) continue;

                // If searching in a base type, the field must be accessible (protected or public)
                if (isBaseType && field.DeclaredAccessibility is not (Accessibility.Protected or Accessibility.ProtectedOrInternal or Accessibility.Public or Accessibility.Internal))
                    continue;

                if (types.IsLogger(field.Type))
                {
                    return field.Name;
                }

                if (field.CanBeReferencedByName)
                {
                    shadowedNames.Add(field.Name);
                }
            }
            currentType = currentType.BaseType;
            isBaseType = true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var constructor in namedType.InstanceConstructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (types.IsLogger(parameter.Type) && !shadowedNames.Contains(parameter.Name))
                    {
                        isFromParameter = true;
                        return parameter.Name;
                    }
                }
            }
        }

        return null;
    }
}
