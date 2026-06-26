using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Esolang.Piet.Generator.Tests;

public class PartialMethodConstraintTests
{
    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_NonPartialMethod_ReportsError(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public void RunSync() { }
            }
            """;

        var compilation = CSharpCompilation.Create("Test",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var generator = new MethodGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics, CancellationToken);

        await Assert.That(diagnostics).Contains(v => v.Id is "PT0013").Because("Expected diagnostic PT0013 (Method must be partial)");
    }
}
