#if NET462_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

// .NET Framework 4.8.1 に元からある同名の属性を、
// アセンブリ(AttributeTargets.Assembly)にも付与できるように上書き定義する
[AttributeUsage(
    AttributeTargets.Assembly |
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Constructor |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Interface |
    AttributeTargets.Event,
    Inherited = false,
    AllowMultiple = false)]
sealed class ExcludeFromCodeCoverageAttribute : Attribute
{
}
#endif
