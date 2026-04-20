using Microsoft.CodeAnalysis;

namespace Esolang.Piet.Generator;

/// <summary>
/// Provides diagnostic definitions reported during source generation.
/// </summary>
public static class DiagnosticDescriptors
{
    const string Category = "Piet";

    /// <summary>
    /// PT0001: Invalid image path parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidImagePathParameter = new(
        id: "PT0001",
        title: "Invalid image path parameter",
        messageFormat: "The image path parameter of the attribute on the method '{0}' must not be null or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0002: Unsupported return type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidReturnType = new(
        id: "PT0002",
        title: "Unsupported return type",
        messageFormat: "The method return type '{0}' is not supported for Piet code generation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0003: Unsupported parameter type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidParameter = new(
        id: "PT0003",
        title: "Unsupported parameter type",
        messageFormat: "The parameter '{0}' of the method has an unsupported type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0004: Duplicate parameter type.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateParameter = new(
        id: "PT0004",
        title: "Duplicate parameter type",
        messageFormat: "The method '{0}' has duplicate parameter types",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0005: Image file not found.
    /// </summary>
    public static readonly DiagnosticDescriptor ImageFileNotFound = new(
        id: "PT0005",
        title: "Image file not found",
        messageFormat: "The Piet image file '{0}' could not be found",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0006: Invalid image format.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidImageFormat = new(
        id: "PT0006",
        title: "Invalid image format",
        messageFormat: "The file '{0}' is not a valid image format for Piet",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0007: Required output interface not provided.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredOutputInterface = new(
        id: "PT0007",
        title: "Required output interface not provided",
        messageFormat: "The Piet program requires output, but the method does not provide an output mechanism",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0008: Required input interface not provided.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredInputInterface = new(
        id: "PT0008",
        title: "Required input interface not provided",
        messageFormat: "The Piet program requires input, but the method does not provide an input mechanism",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0009: Duplicate image path mapping.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateImagePathMapping = new(
        id: "PT0009",
        title: "Duplicate image path mapping",
        messageFormat: "The Piet image path '{0}' is mapped by multiple AdditionalFiles entries",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0010: Input interface provided but not required.
    /// </summary>
    public static readonly DiagnosticDescriptor UnusedInputInterface = new(
        id: "PT0010",
        title: "Input interface provided but not required",
        messageFormat: "The Piet program does not require input, but the method provides an input mechanism",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0011: Return type and output parameter cannot be combined.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnOutputConflict = new(
        id: "PT0011",
        title: "Return type and output parameter conflict",
        messageFormat: "The method '{0}' cannot combine a non-void return type with an explicit output parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PT0012: Language version may be too low for generated code.
    /// </summary>
    public static readonly DiagnosticDescriptor LanguageVersionTooLow = new(
        id: "PT0012",
        title: "Language version may be too low",
        messageFormat: "The method '{0}' may require C# 8.0 or later, but the current language version is '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
