using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Esolang.Piet.Generator.Tests;

[TestClass]
public class MethodGeneratorTests(TestContext TestContext)
{
    // Minimal 1×1 RGB PNG with LightRed (0xFF,0xC0,0xC0). CRC fields are zeroed (not validated by decoder).
    static readonly byte[] MinimalLightRedPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D,                          // IHDR length=13
        0x49, 0x48, 0x44, 0x52,                          // IHDR
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,  // width=1, height=1
        0x08, 0x02, 0x00, 0x00, 0x00,                    // 8-bit RGB
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x0F,                          // IDAT length=15
        0x49, 0x44, 0x41, 0x54,                          // IDAT
        0x78, 0x01,                                      // zlib header
        0x01, 0x04, 0x00, 0xFB, 0xFF,                    // deflate stored block
        0x00, 0xFF, 0xC0, 0xC0,                          // filter=None, R=0xFF,G=0xC0,B=0xC0
        0x05, 0x41, 0x02, 0x80,                          // Adler-32
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x00,                          // IEND length=0
        0x49, 0x45, 0x4E, 0x44,                          // IEND
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
    ];

    // 2×1 RGB PNG: LightRed(0xFF,0xC0,0xC0) → Magenta(0xFF,0x00,0xFF).

    [TestMethod]
    public void Generator_RuntimeInternalMembers_AreEditorBrowsableNever()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void RunSync();

                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial System.Threading.Tasks.Task<string> RunAsync();

                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial System.Collections.Generic.IEnumerable<byte> RunEnumerable();

                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial System.Collections.Generic.IAsyncEnumerable<byte> RunAsyncEnumerable(System.Threading.CancellationToken ct = default);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var runtimeSource = runResult.Results
            .SelectMany(static r => r.GeneratedSources)
            .Single(static s => s.HintName == MethodGenerator.GeneratePietRuntimeFileName)
            .SourceText
            .ToString();

        const string marker = "[EditorBrowsable(EditorBrowsableState.Never)]";
        var occurrenceCount = 0;
        var currentIndex = 0;
        while ((currentIndex = runtimeSource.IndexOf(marker, currentIndex, StringComparison.Ordinal)) >= 0)
        {
            occurrenceCount++;
            currentIndex += marker.Length;
        }

        Assert.AreEqual(
            5,
            occurrenceCount,
            "Expected EditorBrowsable(Never) on runtime class and each internal runtime entrypoint method.");

        AssertNoErrors(outputCompilation);
    }
    // Transition produces hDiff=5,lDiff=1 → cmd 16 (out number). Used to test PT0007.
    static readonly byte[] TwoPixelOutputPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D,                          // IHDR length=13
        0x49, 0x48, 0x44, 0x52,                          // IHDR
        0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01,  // width=2, height=1
        0x08, 0x02, 0x00, 0x00, 0x00,                    // 8-bit RGB
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x12,                          // IDAT length=18
        0x49, 0x44, 0x41, 0x54,                          // IDAT
        0x78, 0x01,                                      // zlib header
        0x01, 0x07, 0x00, 0xF8, 0xFF,                    // deflate stored block (LEN=7)
        0x00, 0xFF, 0xC0, 0xC0, 0xFF, 0x00, 0xFF,        // filter=None, LightRed, Magenta
        0x00, 0x00, 0x00, 0x00,                          // Adler-32 (not validated)
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x00,                          // IEND length=0
        0x49, 0x45, 0x4E, 0x44,                          // IEND
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
    ];

    // 2×1 RGB PNG: LightRed(0xFF,0xC0,0xC0) → DarkBlue(0x00,0x00,0xC0).
    // Transition produces hDiff=4,lDiff=2 → cmd 14 (in number). Used to test PT0008.
    static readonly byte[] TwoPixelInputPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D,                          // IHDR length=13
        0x49, 0x48, 0x44, 0x52,                          // IHDR
        0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01,  // width=2, height=1
        0x08, 0x02, 0x00, 0x00, 0x00,                    // 8-bit RGB
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x12,                          // IDAT length=18
        0x49, 0x44, 0x41, 0x54,                          // IDAT
        0x78, 0x01,                                      // zlib header
        0x01, 0x07, 0x00, 0xF8, 0xFF,                    // deflate stored block (LEN=7)
        0x00, 0xFF, 0xC0, 0xC0, 0x00, 0x00, 0xC0,        // filter=None, LightRed, DarkBlue
        0x00, 0x00, 0x00, 0x00,                          // Adler-32 (not validated)
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
        0x00, 0x00, 0x00, 0x00,                          // IEND length=0
        0x49, 0x45, 0x4E, 0x44,                          // IEND
        0x00, 0x00, 0x00, 0x00,                          // CRC (placeholder)
    ];

    static readonly byte[] MinimalLightRed2x2Png = BuildStoredRgbPng(2, 2, [
        0x00, 0xFF,0xC0,0xC0, 0xFF,0xC0,0xC0,
        0x00, 0xFF,0xC0,0xC0, 0xFF,0xC0,0xC0
    ]);

    static string MakeTransformedText(string logicalPath, byte[] pngBytes) =>
        $"// PIET_IMAGE_PATH={logicalPath}\n// PIET_CODEL_SIZE=1\n{Convert.ToBase64String(pngBytes)}";

    static byte[] BuildStoredRgbPng(int width, int height, byte[] rawScanlineBytes)
    {
        static void WriteInt32BE(List<byte> list, int value)
        {
            list.Add((byte)((value >> 24) & 0xFF));
            list.Add((byte)((value >> 16) & 0xFF));
            list.Add((byte)((value >> 8) & 0xFF));
            list.Add((byte)(value & 0xFF));
        }

        var png = new List<byte>();
        png.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        WriteInt32BE(png, 13);
        png.AddRange(new byte[] { 0x49, 0x48, 0x44, 0x52 });
        WriteInt32BE(png, width);
        WriteInt32BE(png, height);
        png.AddRange(new byte[] { 0x08, 0x02, 0x00, 0x00, 0x00 });
        png.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        var idatPayload = new List<byte>
        {
            0x78, 0x01,
            0x01,
            (byte)(rawScanlineBytes.Length & 0xFF),
            (byte)((rawScanlineBytes.Length >> 8) & 0xFF),
            (byte)(~rawScanlineBytes.Length & 0xFF),
            (byte)((~rawScanlineBytes.Length >> 8) & 0xFF),
        };
        idatPayload.AddRange(rawScanlineBytes);
        idatPayload.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        WriteInt32BE(png, idatPayload.Count);
        png.AddRange(new byte[] { 0x49, 0x44, 0x41, 0x54 });
        png.AddRange(idatPayload);
        png.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        WriteInt32BE(png, 0);
        png.AddRange(new byte[] { 0x49, 0x45, 0x4E, 0x44 });
        png.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });

        return png.ToArray();
    }
    CancellationToken CancellationToken => TestContext.CancellationToken;
    void LogWriteLine(string message) => TestContext.WriteLine(message);

    void LogDiagnostics(ImmutableArray<Diagnostic>? diagnostics)
    {
        if (diagnostics is not { Length: > 0 }) return;
        LogWriteLine(string.Join("\n", Enumerable.Select((IEnumerable<Diagnostic>)diagnostics, static d => d.ToString())));
    }

    void LogDiagnostics(Compilation? compilation) => LogDiagnostics(compilation?.GetDiagnostics(CancellationToken));

    void LogDiagnostics(ImmutableArray<Diagnostic>? diagnostics, Compilation? compilation)
    {
        LogDiagnostics(diagnostics);
        LogDiagnostics(compilation);
    }
    void LogSyntaxTrees(Compilation? compilation)
    {
        if (compilation is null) return;
        LogWriteLine(string.Join("\n",
            compilation.SyntaxTrees
                .Select(tree => $"// {tree.FilePath}\n{tree.GetText(CancellationToken)}")
            )
        );
    }

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

        var referenceList = references.ToList();
        {
            var hasPipelinesReference = referenceList.Any(static r =>
                string.Equals(Path.GetFileNameWithoutExtension(r.FilePath), "System.IO.Pipelines", StringComparison.OrdinalIgnoreCase));
            if (!hasPipelinesReference)
            {
                var pipelinesAssemblyLocation = typeof(System.IO.Pipelines.PipeReader).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(pipelinesAssemblyLocation))
                {
                    referenceList.Add(MetadataReference.CreateFromFile(pipelinesAssemblyLocation));
                }
            }
        }
        {
            var assemblyLocation = typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(assemblyLocation))
            {
                referenceList.Add(MetadataReference.CreateFromFile(assemblyLocation));
            }
        }
#if !NET
        {
            var pipelinesAssemblyLocation = typeof(Memory<>).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(pipelinesAssemblyLocation))
            {
                referenceList.Add(MetadataReference.CreateFromFile(pipelinesAssemblyLocation));
            }
        }
        {
            var asm = typeof(ValueTask).Assembly.Location;
            referenceList.Add(MetadataReference.CreateFromFile(asm));
        }
        {
            var asm = typeof(IAsyncEnumerable<>).Assembly.Location;
            referenceList.Add(MetadataReference.CreateFromFile(asm));
        }
#endif


        baseCompilation = CSharpCompilation.Create(
            "generator-tests",
            references: referenceList,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                specificDiagnosticOptions: new[] { 
                    new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress) 
                }
            )
        );
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

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()")),
            "Expected generated method implementation was not found.");

        AssertNoErrors(outputCompilation);
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

    [TestMethod]
    public void Generator_WithMissingImageFile_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("missing.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0005"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithInvalidImageFormat_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("invalid.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("invalid.png", null));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0006"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTransformedImageText_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()")),
            "Expected generated method implementation was not found.");

        Assert.IsFalse(
            outputCompilation.GetDiagnostics(CancellationToken).Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator.\n"
            + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithStringReturn_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial string Run();
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("return")),
            "Expected generated string return path was not found.");

        Assert.IsFalse(
            outputCompilation.GetDiagnostics(CancellationToken).Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator.\n"
            + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTextReaderAndTextWriterParameters_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run(System.IO.TextReader input, System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run(global::System.IO.TextReader input, global::System.IO.TextWriter output)")),
            "Expected generated method implementation with TextReader/TextWriter parameters was not found.");

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id is "PT0007" or "PT0008"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));

        Assert.IsFalse(
            outputCompilation.GetDiagnostics(CancellationToken).Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator.\n"
            + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithUnsupportedParameter_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run(int value);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0003"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithMultipleInputKinds_ReportsDuplicateParameterDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run(string input, System.IO.TextReader reader);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0004"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithReturnTypeAndOutputParameter_ReportsReturnOutputConflictDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial string Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTaskReturnAndTextWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        AssertNoErrors(diagnostics);

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should not be reported for Task return + TextWriter parameter.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithTaskReturnAndPipeWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        AssertNoErrors(diagnostics);

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should not be reported for Task return + PipeWriter parameter.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithValueTaskReturnAndTextWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        AssertNoErrors(diagnostics);

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should not be reported for ValueTask return + TextWriter parameter.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithValueTaskReturnAndPipeWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        AssertNoErrors(diagnostics);

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should not be reported for ValueTask return + PipeWriter parameter.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithInvalidTransformedImageText_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = "// PIET_IMAGE_PATH=samples/hello-world.png\ninvalid-base64";
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0006"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithOutputImage_AndNoOutputMechanism_ReportsDiagnostic()
    {
        // void method with no output parameter/return value → should report PT0007
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0007"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithOutputImage_AndStringReturn_DoesNotReportMissingOutputDiagnostic()
    {
        // string return → explicit output mechanism → no PT0007
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial string Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0007"),
            "PT0007 should not be reported when the method has a string return type.");
    }

    [TestMethod]
    public void Generator_WithInputImage_AndNoInputMechanism_ReportsDiagnostic()
    {
        // void method with no input parameter → should report PT0008
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0008"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithInputImage_AndPipeReaderParameter_DoesNotReportMissingInputDiagnostic()
    {
        // PipeReader parameter → explicit input mechanism → no PT0008
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run(System.IO.Pipelines.PipeReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0008"),
            "PT0008 should not be reported when the method has a PipeReader parameter.");
    }

    [TestMethod]
    public void Generator_WithInputImage_AndTextReaderParameter_DoesNotReportMissingInputDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run(System.IO.TextReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0008"),
            "PT0008 should not be reported when the method has a TextReader parameter.");
    }

    [TestMethod]
    public void Generator_WithInputImage_AndStringParameter_DoesNotReportMissingInputDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run(string input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0008"),
            "PT0008 should not be reported when the method has a string parameter.");
    }

    [TestMethod]
    public void Generator_WithStringInputParameter_GeneratesStringReaderInputAdapter()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial System.Threading.Tasks.Task<string> Run(string input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generatedText = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

        Assert.IsTrue(generatedText.Contains("new global::System.IO.StringReader(input)"),
            "Expected StringReader adapter was not found in generated code.");
        Assert.IsTrue(generatedText.Contains("global::System.Threading.Tasks.ValueTask<int?> __pietReadNumberAsync"),
            "Expected async input delegate for string input was not found.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithTextReaderInputAndTaskReturn_GeneratesAsyncTextReaderPath()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial System.Threading.Tasks.Task<string> Run(System.IO.TextReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generatedText = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

        Assert.IsTrue(generatedText.Contains("ReadLineAsync(__ct)"),
            "Expected TextReader.ReadLineAsync path was not found in generated code.");
        Assert.IsTrue(generatedText.Contains("__pietReadNumberAsyncAwaited"),
            "Expected async awaited fallback path for TextReader input was not found.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithPipeReaderInput_GeneratesSyncPipeReaderPath()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial string Run(System.IO.Pipelines.PipeReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generatedText = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

        Assert.IsTrue(generatedText.Contains("ReadAsync(default(global::System.Threading.CancellationToken)).AsTask().GetAwaiter().GetResult();"),
            "Expected synchronous PipeReader.ReadAsync bridge path was not found in generated code.");
        Assert.IsTrue(generatedText.Contains("global::System.Buffers.BuffersExtensions.ToArray(__buffer)"),
            "Expected PipeReader number-read conversion path was not found in generated code.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithPipeReaderInputAndTaskReturn_GeneratesAsyncPipeReaderPath()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial System.Threading.Tasks.Task<string> Run(System.IO.Pipelines.PipeReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generatedText = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

        Assert.IsTrue(generatedText.Contains("async global::System.Threading.Tasks.ValueTask<int?> __pietReadNumberAsync"),
            "Expected async PipeReader number-read delegate was not found in generated code.");
        Assert.IsTrue(generatedText.Contains("await input.ReadAsync(__ct).ConfigureAwait(false);"),
            "Expected PipeReader.ReadAsync await path was not found in generated code.");
        Assert.IsTrue(generatedText.Contains("global::System.Buffers.BuffersExtensions.ToArray(__buffer)"),
            "Expected PipeReader buffer conversion path was not found in generated code.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithOutputImage_AndTextWriterParameter_DoesNotReportMissingOutputDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run(System.IO.TextWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsFalse(
            generatorDiagnostics.Any(static x => x.Id == "PT0007"),
            "PT0007 should not be reported when the method has a TextWriter parameter.");
    }

    [TestMethod]
    public void Generator_WithUnusedInputParameter_ReportsHiddenDiagnostic_AndGeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run(System.IO.Pipelines.PipeReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0010" && x.Severity == DiagnosticSeverity.Hidden),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run(global::System.IO.Pipelines.PipeReader input)")),
            "Expected generated method implementation was not found.");

        AssertNoErrors(diagnostics);

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithTaskStringReturn_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task<string> Run();
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generated = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .Single(static text => text.Contains("partial class Sample"));

        Assert.Contains("public async partial global::System.Threading.Tasks.Task<string> Run(", generated,
            "Expected async Task<string> signature was not found.");

        Assert.IsTrue(generated.Contains("PietRuntime.ExecuteAsync("),
            "Expected async runtime call was not found.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithIEnumerableByteReturn_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Collections.Generic.IEnumerable<byte> Run();
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("yield return")),
            "Expected yield return byte path was not found.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithCancellationTokenParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run(System.Threading.CancellationToken ct = default);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("CancellationToken ct")),
            "Expected CancellationToken parameter was not found in generated code.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithoutGeneratePietMethodAttribute_DoesNotEmitMethodSource()
    {
        const string source = "public class Sample { public void Run() { } }";

        var driver = RunGeneratorsAndUpdateCompilation(source, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();
        var generatedHints = runResult.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.HintName).ToArray();

        AssertNoErrors(diagnostics);

        Assert.IsFalse(generatedHints.Contains(MethodGenerator.GeneratedMethodsFileName, StringComparer.Ordinal));
        Assert.IsFalse(generatedHints.Contains(MethodGenerator.GeneratePietRuntimeFileName, StringComparer.Ordinal));

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithInvalidReturnType_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial double Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0002"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithIntReturn_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial int Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();

            AssertNoErrors(diagnostics);

            Assert.IsTrue(
                runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial int Run()")),
                "Expected generated int return method implementation was not found.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithIntExitCodePatterns_ReturnsZero()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public static partial int RunInt();

                [Esolang.Piet.GeneratePietMethod("program.png")]
                public static partial System.Threading.Tasks.Task<int> RunTaskInt();

                [Esolang.Piet.GeneratePietMethod("program.png")]
                public static partial System.Threading.Tasks.ValueTask<int> RunValueTaskInt();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();

            AssertNoErrors(diagnostics);
            AssertNoErrors(outputCompilation);

            var generatedText = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText(CancellationToken).ToString()));
            Assert.IsTrue(generatedText.Contains("RunInt("), "Expected RunInt method was not found.");
            Assert.IsTrue(generatedText.Contains("RunTaskInt("), "Expected RunTaskInt method was not found.");
            Assert.IsTrue(generatedText.Contains("RunValueTaskInt("), "Expected RunValueTaskInt method was not found.");
            Assert.IsTrue(generatedText.Contains("return 0;"), "Expected exit-code return statement was not found.");
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithGenericMethod_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run<T>();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0003"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithRefParameter_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run(ref int value);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0003"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithDuplicateCancellationToken_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run(System.Threading.CancellationToken ct1, System.Threading.CancellationToken ct2);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0004"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithStringAndPipeReader_ReportsDuplicateParameterDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run(string input, System.IO.Pipelines.PipeReader reader);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0004"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithDuplicatePipeWriter_ReportsDuplicateParameterDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run(System.IO.Pipelines.PipeWriter output1, System.IO.Pipelines.PipeWriter output2);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0004"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithDuplicateTextWriter_ReportsDuplicateParameterDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run(System.IO.TextWriter output1, System.IO.TextWriter output2);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0004"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithGlobalNamespaceAndStaticMethod_GeneratesMethod()
    {
        const string source = """
            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public static partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt",
                MakeTransformedText("program.png", MinimalLightRedPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var generatedMethod = runResult.GeneratedTrees
            .Select(tree => tree.GetText(CancellationToken).ToString())
            .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

        Assert.IsTrue(generatedMethod.Contains("public static partial"), "Expected static modifier was not found.");
        Assert.IsFalse(generatedMethod.Contains("namespace "), "Global namespace method should not emit a namespace declaration.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithUnreadableTransformedText_ReportsImageNotFoundDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", null));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
                generatorDiagnostics.Any(static x => x.Id == "PT0005"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTransformedTextWithoutPrefix_ReportsImageNotFoundDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = "// NOT_PIET=hello-world.png\n" + Convert.ToBase64String(MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
                generatorDiagnostics.Any(static x => x.Id == "PT0005"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTransformedTextWithEmptyImagePath_ReportsImageNotFoundDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = "// PIET_IMAGE_PATH=   \n// PIET_CODEL_SIZE=1\n" + Convert.ToBase64String(MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
                generatorDiagnostics.Any(static x => x.Id == "PT0005"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTransformedTextWithoutNewline_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = "// PIET_IMAGE_PATH=hello-world.png\n// PIET_CODEL_SIZE=1\n";
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0006"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithTransformedTextWithEmptyPayload_ReportsDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial void Run();
            }
            """;

        var transformed = "// PIET_IMAGE_PATH=hello-world.png\n// PIET_CODEL_SIZE=1\n   ";
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0006"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithVerticalOutputTransition_ReportsMissingOutputDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("vertical-output.png")]
                public partial void Run();
            }
            """;

        var verticalOutputPng = BuildStoredRgbPng(1, 2, new byte[]
        {
            0x00, 0xFF, 0xC0, 0xC0,
            0x00, 0xFF, 0x00, 0xFF,
        });
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/vertical-output.png.piet.txt",
                MakeTransformedText("vertical-output.png", verticalOutputPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0007"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithBlackPixelProgram_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("black.png")]
                public partial void Run();
            }
            """;

        var blackPng = BuildStoredRgbPng(1, 1,
        [
            0x00, 0x00, 0x00, 0x00,
        ]);

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/black.png.piet.txt",
                MakeTransformedText("black.png", blackPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()")),
            "Expected generated method implementation was not found.");

        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithCSharp12_ParseOptions_CompilesGeneratedCode()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("black.png")]
                public partial void Run();
            }
            """;

        var blackPng = BuildStoredRgbPng(1, 1, new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
        });

        _ = RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out var outputCompilation,
            out var diagnostics,
            LanguageVersion.CSharp12,
            new TestAdditionalText("obj/black.png.piet.txt",
                MakeTransformedText("black.png", blackPng)));

        AssertNoErrors(diagnostics);

        var diagnostics2 = outputCompilation.GetDiagnostics(CancellationToken);
        Assert.IsFalse(
            diagnostics2.Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator with C#12 parse options.\r\n"
            + string.Join("\r\n", diagnostics2.Select(v => v.ToString())));
    }

    [TestMethod]
    public void Generator_WithLanguageVersionBelowCSharp8_ReportsLanguageVersionWarning()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("black.png")]
                public partial void Run();
            }
            """;

        var blackPng = BuildStoredRgbPng(1, 1, new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
        });

        var driver = RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out _,
            out _,
            LanguageVersion.CSharp7_3,
            new TestAdditionalText("obj/black.png.piet.txt",
                MakeTransformedText("black.png", blackPng)));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0012" && x.Severity == DiagnosticSeverity.Warning),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public void Generator_WithValueTaskStringReturn_GeneratesMethod()
    {
#if NET8_0_OR_GREATER
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask<string> Run();
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("public async partial global::System.Threading.Tasks.ValueTask<string> Run()"))
            && runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("return __pietString;")),
            "Expected ValueTask<string> return path was not found.");

        AssertNoErrors(outputCompilation);
#else
        Assert.Inconclusive("ValueTask return type support requires .NET 8 or later.");
#endif
    }

    [TestMethod]
    public void Generator_WithIAsyncEnumerableByteReturn_GeneratesAsyncMethod()
    {

        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Collections.Generic.IAsyncEnumerable<byte> Run(System.Threading.CancellationToken ct = default);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();

            AssertNoErrors(diagnostics);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("IAsyncEnumerable")) ?? string.Empty;

            Assert.IsTrue(generatedText.Contains("public async partial global::System.Collections.Generic.IAsyncEnumerable<byte> Run([global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken ct)"), "Expected async modifier in generated code.");
            Assert.IsTrue(generatedText.Contains("yield return __pietByte;"), "Expected yield return byte path was not found.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithPipeReaderInput_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial string Run(System.IO.Pipelines.PipeReader input);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        var pipeReaderErrors = outputCompilation.GetDiagnostics(CancellationToken)
            .Where(static x => x.Severity == DiagnosticSeverity.Error)
            .Select(static x => x.ToString())
            .ToArray();

        Assert.IsFalse(
            pipeReaderErrors.Length > 0,
            "Compilation contains errors after running generator. " + string.Join(" | ", pipeReaderErrors));
    }

    [TestMethod]
    public void Generator_WithPipeWriterOutput_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));
        var runResult = driver.GetRunResult();

        AssertNoErrors(diagnostics);

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree => tree.GetText(CancellationToken).ToString().Contains("global::System.Buffers.BuffersExtensions.Write(output, ")),
            "Expected PipeWriter.WriteAsync() was not found in generated code.");

        var pipeWriterErrors = outputCompilation.GetDiagnostics(CancellationToken)
            .Where(static x => x.Severity == DiagnosticSeverity.Error)
            .Select(static x => x.ToString())
            .ToArray();

        Assert.IsFalse(
            pipeWriterErrors.Length > 0,
            "Compilation contains errors after running generator. " + string.Join(" | ", pipeWriterErrors));
    }

    [TestMethod]
    [DataRow("hello-world.png", "samples", "Generator.UseConsole", "samples", "hello-world.png")]
    [DataRow("ascii-piet-sample.txt", "samples", "Generator.UseConsole", "samples", "ascii-piet-sample.txt")]
    [DataRow("ppm-sample.ppm", "samples", "Generator.UseConsole", "samples", "ppm-sample.ppm")]
    [DataRow("dot.gif", "samples", "Generator.UseConsole", "samples", "dot.gif")]
    [DataRow("dot-codel-11.gif", "samples", "Generator.UseConsole", "samples", "dot-codel-11.gif")]
    public void Generator_WithSampleConformanceVector_GeneratesWithoutDiagnostics(
        string logicalPath,
        string p1,
        string p2,
        string p3,
        string p4)
    {
        var samplePath = FindFileInRepository(p1, p2, p3, p4);
        var transformed = MakeTransformedText(logicalPath, File.ReadAllBytes(samplePath));
        var source = $$"""
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("{{logicalPath}}")]
                public partial string Run();
            }
            """;

        _ = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText($"obj/{logicalPath}.piet.txt", transformed));

        AssertNoErrors(diagnostics);
        AssertNoErrors(outputCompilation);
    }

    [TestMethod]
    public void Generator_WithInputOutputSampleAndNoInputMechanism_ReportsDiagnostic()
    {
        const string logicalPath = "input-output.png";
        var samplePath = FindFileInRepository("samples", "Generator.UseConsole", "samples", logicalPath);
        var transformed = MakeTransformedText(logicalPath, File.ReadAllBytes(samplePath));
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input-output.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input-output.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0008"),
            string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [TestMethod]
    public async Task Generator_WithLoggerParameter_LogsExecution()
    {
        const string source = """
            namespace Demo;

            public class FakeLogger : Microsoft.Extensions.Logging.ILogger
            {
                public System.Collections.Generic.List<string> Logs = new System.Collections.Generic.List<string>();
                public System.IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
                public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
                {
                    Logs.Add(formatter(state, exception));
                }
            }

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run(FakeLogger logger);
            }
            """;

        var transformed = MakeTransformedText("program.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed));
        try
        {

            AssertNoErrors(diagnostics);
            AssertNoErrors(outputCompilation);

            var runResult = driver.GetRunResult();
            AssertNoErrors(runResult.Diagnostics);

            var asm = Emit(outputCompilation, CancellationToken);
            var t = asm.GetType("Demo.Sample")!;
            var instance = System.Activator.CreateInstance(t)!;
            var typelogger = asm.GetType("Demo.FakeLogger");
            Assert.IsNotNull(typelogger);
            var logger = Activator.CreateInstance(typelogger);
            Assert.IsNotNull(logger);
            var logs = typelogger.GetField("Logs")?.GetValue(logger) as List<string>;
            Assert.IsNotNull(logs);

            var m = t.GetMethod("Run");
            Assert.IsNotNull(m);
            m.Invoke(instance, [logger]);
            Assert.IsNotEmpty(logs, "No logs were captured.");
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public async Task Generator_WithLoggerPrimaryConstructor_LogsExecution()
    {
        const string source = """
            namespace Demo;

            public class FakeLogger : Microsoft.Extensions.Logging.ILogger
            {
                public System.Collections.Generic.List<string> Logs = new System.Collections.Generic.List<string>();
                public System.IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
                public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
                {
                    Logs.Add(formatter(state, exception));
                }
            }

            public partial class Sample(FakeLogger logger)
            {
                [Esolang.Piet.GeneratePietMethod("program.png")]
                public partial void Run();
            }
            """;

        var transformed = MakeTransformedText("program.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed));

        try
        {
            AssertNoErrors(diagnostics);
            AssertNoErrors(outputCompilation);

            var runResult = driver.GetRunResult();

            AssertNoErrors(runResult.Diagnostics);

            var asm = Emit(outputCompilation, CancellationToken);
            var t = asm.GetType("Demo.Sample")!;
            var typelogger = asm.GetType("Demo.FakeLogger");
            Assert.IsNotNull(typelogger);
            var logger = Activator.CreateInstance(typelogger);
            Assert.IsNotNull(logger);
            var logs = typelogger.GetField("Logs")?.GetValue(logger) as List<string>;
            Assert.IsNotNull(logs);
            var instance = Activator.CreateInstance(t, [logger]);
            Assert.IsNotNull(instance);

            var m = t.GetMethod("Run");
            Assert.IsNotNull(m);
            m.Invoke(instance, null);
            Assert.IsNotEmpty(logs, "No logs were captured.");
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }



    Assembly Emit(Compilation compilation, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms, cancellationToken: cancellationToken);
        if (!result.Success)
        {
            foreach (var d in compilation.GetDiagnostics(cancellationToken))
                LogWriteLine($"Diag: {d}");
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                LogWriteLine(d.ToString());
            foreach (var t in compilation.SyntaxTrees)
                LogWriteLine($"// {t.FilePath}\n{t}");
            Assert.Fail("Compilation emit failed");
        }
        ms.Seek(0, SeekOrigin.Begin);

#if NET48
        return Assembly.Load(ms.ToArray());
#else
        var ctx = new System.Runtime.Loader.AssemblyLoadContext(nameof(MethodGeneratorTests), isCollectible: true);
        return ctx.LoadFromStream(ms);
#endif
    }

    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        params AdditionalText[] additionalTexts)
        => RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out outputCompilation,
            out diagnostics,
            LanguageVersion.CSharp14,
            additionalTexts);

    GeneratorDriver RunGeneratorsAndUpdateCompilationWithLanguageVersion(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        LanguageVersion languageVersion,
        params AdditionalText[] additionalTexts)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: CancellationToken);
        var inputCompilation = baseCompilation.AddSyntaxTrees(syntaxTree);

        var sourceGenerator = new MethodGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([sourceGenerator], additionalTexts, parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out outputCompilation, out diagnostics, CancellationToken);
        return driver;
    }

    sealed class TestAdditionalText(string path, string? content) : AdditionalText
    {
        readonly SourceText? sourceText = content is null ? null : SourceText.From(content, Encoding.UTF8);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => sourceText;
    }

    static string FindFileInRepository(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, Path.Combine(relativeParts));
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find file in repository: {Path.Combine(relativeParts)}");
    }

    [TestMethod]
    public void Generator_WithCodelSizeAttribute_OverridesAdditionalFile()
    {
        // 追加ファイルはPIET_CODEL_SIZE=1だが、属性で3を指定した場合は3が優先される
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("program.png", codelSize: 2)]
    public static partial void Run();
}
""";
        // 追加ファイルにはCodelSize=1を埋め込む
        var transformed = "// PIET_IMAGE_PATH=program.png\n// PIET_CODEL_SIZE=1\n" + Convert.ToBase64String(MinimalLightRed2x2Png);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            Assert.IsEmpty(runResult.Diagnostics,
                string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            // 生成コードにcodelSize=2が反映されているか（例: 配列長やコメント等で判定）
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            Assert.IsNotNull(generated, "Method not generated");
            Assert.IsTrue(generated.Contains("codelSize: 2"), "codelSize=2 not reflected in generated code");
            AssertNoErrors(diagnostics);

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithCodelSizeFromAdditionalFile_UsedWhenNoAttribute()
    {
        // 属性でcodelSize指定なし、追加ファイルPIET_CODEL_SIZE=2の場合は2が使われる
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("program.png")]
    public static partial void Run();
}
""";
        var transformed = "// PIET_IMAGE_PATH=program.png\n// PIET_CODEL_SIZE=2\n" + Convert.ToBase64String(MinimalLightRed2x2Png);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            Assert.IsEmpty(runResult.Diagnostics,
                string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            Assert.IsNotNull(generated, "Method not generated");
            Assert.IsTrue(generated.Contains("codelSize: 2"), "codelSize=2 not reflected in generated code");
            AssertNoErrors(diagnostics);

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithInlineAsciiPiet_UsedAttribute()
    {
        // 属性でcodelSize指定なし、追加ファイルPIET_CODEL_SIZE=2の場合は2が使われる
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=1,l_ C")]
    public static partial void Run();
}
""";
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics);
        try
        {
            var runResult = driver.GetRunResult();
            Assert.IsEmpty(runResult.Diagnostics,
                string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            Assert.IsNotNull(generated, "Method not generated");
            Assert.IsTrue(generated.Contains("codelSize: 1"), "codelSize=1 not reflected in generated code");
            AssertNoErrors(diagnostics);

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithInlineAsciiPietBase64DataUri_UsedAttribute()
    {
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=1;base64,bF8gQw==")]
    public static partial void Run();
}
""";

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics);
        try
        {
            var runResult = driver.GetRunResult();
            Assert.IsEmpty(runResult.Diagnostics,
                string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));

            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            Assert.IsNotNull(generated, "Method not generated");
            Assert.IsTrue(generated.Contains("codelSize: 1"), "codelSize=1 not reflected in generated code");
            AssertNoErrors(diagnostics);
            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithInlineAsciiPietInvalidCodelSize_FallsBackToDefault()
    {
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=abc,l_ C")]
    public static partial void Run();
}
""";

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics);
        try
        {
            var runResult = driver.GetRunResult();
            Assert.IsEmpty(runResult.Diagnostics,
                string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));

            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            Assert.IsNotNull(generated, "Method not generated");
            Assert.IsTrue(generated.Contains("codelSize: 1"), "codelSize fallback to default(1) was not reflected in generated code");
            AssertNoErrors(diagnostics);
            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithInlineDataUriMissingPayloadSeparator_ReportsImageNotFound()
    {
        const string source =
"""
namespace Demo;

public partial class Sample
{
    [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=1")]
    public static partial void Run();
}
""";

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0005"));
        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0005")),
            "Expected throw for PT0005 was not generated.");
    }

    [TestMethod]
    public void Generator_WithInvalidImagePath_GeneratesThrowingMethod()
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

        // PT0001 が出る
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0001"));

        // 生成コードに throw が含まれる
        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0001")),
            "Expected throw for PT0001 was not generated.");
    }

    [TestMethod]
    public void Generator_WithMissingImageFile_GeneratesThrowingMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("missing.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _);
        var runResult = driver.GetRunResult();

        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0005"));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0005")),
            "Expected throw for PT0005 was not generated.");
    }

    [TestMethod]
    public void Generator_WithInvalidImageFormat_GeneratesThrowingMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("invalid.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("invalid.png", null));

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0006"));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0006")),
            "Expected throw for PT0006 was not generated.");
    }

    [TestMethod]
    public void Generator_WithOutputImage_AndNoOutputMechanism_GeneratesThrowingMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("output.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/output.png.piet.txt",
                MakeTransformedText("output.png", TwoPixelOutputPng)));

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0007"));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.InvalidOperationException(\"PT0007")),
            "Expected throw for PT0007 was not generated.");
    }

    [TestMethod]
    public void Generator_WithInputImage_AndNoInputMechanism_GeneratesThrowingMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)));

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(generatorDiagnostics.Any(x => x.Id == "PT0008"));

        Assert.IsTrue(
            runResult.GeneratedTrees.Any(tree =>
                tree.GetText(CancellationToken).ToString().Contains("throw new global::System.InvalidOperationException(\"PT0008")),
            "Expected throw for PT0008 was not generated.");
    }

    [TestMethod]
    public void Generator_WithTaskReturnAndTextWriterParameter_GeneratesCorrectMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task Run(System.IO.TextWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt",
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for Task return + TextWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithTaskReturnAndPipeWriterParameter_GeneratesCorrectMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt",
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for Task return + PipeWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithValueTaskReturnAndTextWriterParameter_GeneratesCorrectMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask Run(System.IO.TextWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt",
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for ValueTask return + TextWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithValueTaskReturnAndPipeWriterParameter_GeneratesCorrectMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt",
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for ValueTask return + PipeWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    void AssertNoErrors(ImmutableArray<Diagnostic> diagnostics) =>
        Assert.IsFalse(diagnostics.Any(static x => x.Severity == DiagnosticSeverity.Error),
            string.Join("\n", diagnostics.Select(static x => x.ToString())));

    void AssertNoErrors(Compilation outputCompilation)
    {
        var diagnostics = outputCompilation.GetDiagnostics(CancellationToken);
        Assert.IsFalse(diagnostics.Any(static x => x.Severity == DiagnosticSeverity.Error),
            "Compilation contains errors after running generator." + string.Join("\n", diagnostics.Select(static x => x.ToString())));
    }

    // -----------------------------------------------------------------------
    // int / Task<int> / ValueTask<int> + TextWriter / PipeWriter の組み合わせ
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Generator_WithIntReturnAndTextWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial int Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));

        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for int return + TextWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithIntReturnAndPipeWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial int Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for int return + PipeWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithTaskIntReturnAndTextWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task<int> Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for Task<int> return + TextWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithTaskIntReturnAndPipeWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.Task<int> Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for Task<int> return + PipeWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithValueTaskIntReturnAndTextWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask<int> Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for ValueTask<int> return + TextWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithValueTaskIntReturnAndPipeWriterParameter_GeneratesMethod()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Threading.Tasks.ValueTask<int> Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            AssertNoErrors(diagnostics);

            Assert.IsFalse(
                generatorDiagnostics.Any(static x => x.Id == "PT0011"),
                "PT0011 should not be reported for ValueTask<int> return + PipeWriter parameter.");

            AssertNoErrors(outputCompilation);
        }
        catch
        {
            LogDiagnostics(diagnostics, outputCompilation);
            LogSyntaxTrees(outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public void Generator_WithStringReturnAndPipeWriterParameter_ReportsReturnOutputConflictDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial string Run(System.IO.Pipelines.PipeWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should be reported for string return + PipeWriter parameter.");
    }

    [TestMethod]
    public void Generator_WithIEnumerableByteReturnAndTextWriterParameter_ReportsReturnOutputConflictDiagnostic()
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("hello-world.png")]
                public partial System.Collections.Generic.IEnumerable<byte> Run(System.IO.TextWriter output);
            }
            """;

        var transformed = MakeTransformedText("samples/hello-world.png", MinimalLightRedPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed));
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        Assert.IsTrue(
            generatorDiagnostics.Any(static x => x.Id == "PT0011"),
            "PT0011 should be reported for IEnumerable<byte> return + TextWriter parameter.");
    }
}
