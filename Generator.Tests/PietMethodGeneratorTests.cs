using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Esolang.Piet.Generator.Tests;

[TestClass]
public class MethodGeneratorTests
{
    public TestContext TestContext { get; set; } = default!;
    CancellationToken CancellationToken => TestContext.CancellationToken;
    Compilation baseCompilation = default!;

    [TestInitialize]
    public void InitializeCompilation()
    {
        IEnumerable<PortableExecutableReference> references =
#if NET10_0_OR_GREATER
            Net100.References.All;
#elif NET9_0_OR_GREATER
            Net90.References.All;
#elif NET8_0_OR_GREATER
            Net80.References.All;
#elif NET472_OR_GREATER
            Net472.References.All;
#else
            throw new InvalidOperationException("Unsupported target framework for generator tests.");
#endif

        baseCompilation = CSharpCompilation.Create(
            "generator-tests",
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [TestMethod]
    public void Generator_Initialization_Succeeds()
    {
        var generator = new MethodGenerator();
        Assert.IsNotNull(generator);
    }

    [TestMethod]
    public void Generator_WithValidAttribute_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        Assert.IsFalse(diagnostics.Any(static x => x.Severity == DiagnosticSeverity.Error),
            string.Join("\n", diagnostics.Select(static x => x.ToString())));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(static tree => tree.GetText().ToString().Contains("public partial void Run()")),
            "Expected generated method implementation was not found.");

        Assert.IsFalse(
            outputCompilation.GetDiagnostics(CancellationToken).Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator.");
    }

    [TestMethod]
    public void Generator_WithInvalidImagePath_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0001"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: CancellationToken);
        var inputCompilation = baseCompilation.AddSyntaxTrees(syntaxTree);

        var sourceGenerator = new MethodGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create([sourceGenerator]).WithUpdatedParseOptions(parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out outputCompilation, out diagnostics, CancellationToken);
        return driver;
    }
}
