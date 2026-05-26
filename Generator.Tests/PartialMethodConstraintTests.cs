using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Piet.Generator.Tests;

[TestClass]
public class PartialMethodConstraintTests(TestContext TestContext)
{
#pragma warning disable MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    [TestMethod]
    public void Generator_NonPartialMethod_ReportsError()
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

        Assert.Contains(v => v.Id is "PT0013", diagnostics, "Expected diagnostic PT0013 (Method must be partial)");
    }
}
