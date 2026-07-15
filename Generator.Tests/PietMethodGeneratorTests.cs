using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Exceptions;

namespace Esolang.Piet.Generator.Tests;

public class MethodGeneratorTests
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

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_RuntimeInternalMembers_AreEditorBrowsableNever(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

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

            await Assert
                .That(occurrenceCount)
                .IsEqualTo(5)
                .Because("Expected EditorBrowsable(Never) on runtime class and each internal runtime entrypoint method.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics);
            throw;
        }
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

    static string MakeTransformedText(string logicalPath, byte[] pngBytes, string language = "Piet") =>
        $"// PIET_IMAGE_PATH={logicalPath}\n// PIET_CODEL_SIZE=1\n// PIET_LANGUAGE={language}\n{Convert.ToBase64String(pngBytes)}";

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
        png.AddRange([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        WriteInt32BE(png, 13);
        png.AddRange([0x49, 0x48, 0x44, 0x52]);
        WriteInt32BE(png, width);
        WriteInt32BE(png, height);
        png.AddRange([0x08, 0x02, 0x00, 0x00, 0x00]);
        png.AddRange([0x00, 0x00, 0x00, 0x00]);

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
        idatPayload.AddRange([0x00, 0x00, 0x00, 0x00]);

        WriteInt32BE(png, idatPayload.Count);
        png.AddRange([0x49, 0x44, 0x41, 0x54]);
        png.AddRange(idatPayload);
        png.AddRange([0x00, 0x00, 0x00, 0x00]);

        WriteInt32BE(png, 0);
        png.AddRange([0x49, 0x45, 0x4E, 0x44]);
        png.AddRange([0x00, 0x00, 0x00, 0x00]);

        return [.. png];
    }
    void LogWriteLine(string message) => TestContext.OutputWriter.WriteLine(message);

    void LogDiagnostics(ImmutableArray<Diagnostic>? diagnostics)
    {
        if (diagnostics is not { Length: > 0 }) return;
        LogWriteLine(string.Join("\n", Enumerable.Select((IEnumerable<Diagnostic>)diagnostics, static d => d.ToString())));
    }

    void LogDiagnostics(Compilation? compilation, CancellationToken CancellationToken) => LogDiagnostics(compilation?.GetDiagnostics(CancellationToken));

    void LogDiagnostics(ImmutableArray<Diagnostic>? diagnostics, Compilation? compilation, CancellationToken CancellationToken)
    {
        LogDiagnostics(diagnostics);
        LogDiagnostics(compilation, CancellationToken);
        LogSyntaxTrees(compilation, CancellationToken);
    }

    void LogSyntaxTrees(Compilation? compilation, CancellationToken CancellationToken)
    {
        if (compilation is null) return;
        LogWriteLine(string.Join("\n",
            compilation.SyntaxTrees
                .Select(tree => $"// {tree.FilePath}\n{tree.GetText(CancellationToken)}")
            )
        );
    }

    readonly Compilation baseCompilation = default!;

    readonly TestContext TestContext;
    public MethodGeneratorTests()
    {
        this.TestContext = TestContext.Current!;
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
                specificDiagnosticOptions: [
                    new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress)
                ]
            )
        );
    }

    [Test]
    public async Task Generator_Initialization_Succeeds()
    {
        var generator = new MethodGenerator();
        await Assert.That(generator).IsNotNull();
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithValidAttribute_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()"))
                .Because("Expected generated method implementation was not found.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithInvalidImagePath_ReportsDiagnostic(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0001")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithMissingImageFile_ReportsDiagnostic(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("missing.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0005")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithInvalidImageFormat_ReportsDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("invalid.png", null),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0006")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithTransformedImageText_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()"))
                .Because("Expected generated method implementation was not found.");

            await Assert.That(outputCompilation.GetDiagnostics(CancellationToken))
                .DoesNotContain(static x => x.Severity == DiagnosticSeverity.Error)
                .Because("Compilation contains errors after running generator.\n"
                + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithStringReturn_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("return"))
                .Because("Expected generated string return path was not found.");

            await Assert.That(outputCompilation.GetDiagnostics(CancellationToken))
                .DoesNotContain(static x => x.Severity == DiagnosticSeverity.Error)
                .Because("Compilation contains errors after running generator.\n"
                + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithTextReaderAndTextWriterParameters_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run(global::System.IO.TextReader input, global::System.IO.TextWriter output)"))
                .Because("Expected generated method implementation with TextReader/TextWriter parameters was not found.");

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id is "PT0007" or "PT0008")
                .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));

            await Assert.That(outputCompilation.GetDiagnostics(CancellationToken))
                .DoesNotContain(static x => x.Severity == DiagnosticSeverity.Error)
                .Because("Compilation contains errors after running generator.\n"
                + string.Join("\n", outputCompilation.GetDiagnostics(CancellationToken).Select(static x => x.ToString())));
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithUnsupportedParameter_ReportsDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0003")
            .Because(string.Join("\n", string.Join("\n", generatorDiagnostics.Select(static x => x.ToString()))));
    }

    [Test]
    public async Task Generator_WithMultipleInputKinds_ReportsDuplicateParameterDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0003")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithReturnTypeAndOutputParameter_ReportsReturnOutputConflictDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0011")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithTaskReturnAndTextWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);


            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task return + TextWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTaskReturnAndPipeWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task return + PipeWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    public async Task Generator_WithValueTaskReturnAndTextWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask return + TextWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithValueTaskReturnAndPipeWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask return + PipeWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or InvalidOperationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithInvalidTransformedImageText_ReportsDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0006")
            .Because("PT0006 should be reported for invalid transformed image text.");
    }

    [Test]
    public async Task Generator_WithOutputImage_AndNoOutputMechanism_ReportsDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0007")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithOutputImage_AndStringReturn_DoesNotReportMissingOutputDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .DoesNotContain(static x => x.Id == "PT0007")
            .Because("PT0007 should not be reported when the method has a string return type.");
    }

    [Test]
    public async Task Generator_WithInputImage_AndNoInputMechanism_ReportsDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0008")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithInputImage_AndPipeReaderParameter_DoesNotReportMissingInputDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .DoesNotContain(static x => x.Id == "PT0008")
            .Because("PT0008 should not be reported when the method has a PipeReader parameter.");
    }

    [Test]
    public async Task Generator_WithInputImage_AndTextReaderParameter_DoesNotReportMissingInputDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .DoesNotContain(static x => x.Id == "PT0008")
            .Because("PT0008 should not be reported when the method has a TextReader parameter.");
    }

    [Test]
    public async Task Generator_WithInputImage_AndStringParameter_DoesNotReportMissingInputDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .DoesNotContain(static x => x.Id == "PT0008")
            .Because("PT0008 should not be reported when the method has a string parameter.");
    }

    [Test]
    public async Task Generator_WithStringInputParameter_GeneratesStringReaderInputAdapter(CancellationToken CancellationToken)
    {
        const string source = """
            #nullable enable
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;
            await Assert.That(generatedText)
                .Contains("new global::System.IO.StringReader(input)")
                .Because("Expected StringReader adapter was not found in generated code.");
            await Assert.That(generatedText)
                .Contains("global::System.Threading.Tasks.ValueTask<int?> __pietReadNumberAsync")
                .Because("Expected async input delegate for string input was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    public async Task Generator_WithNullableOptionalStringInputParameter_GeneratesNullableStringReaderInputAdapter(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("input.png")]
                public partial System.Threading.Tasks.Task<string> Run(string? input = null);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/input.png.piet.txt",
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0008")
                .Because("PT0008 should not be reported when the method has a nullable string parameter.");

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;
            await Assert.That(generatedText)
                .Contains("Run(string? input)")
                .Because("Expected generated method signature to preserve nullable string input.");
            await Assert.That(generatedText)
                .Contains("new global::System.IO.StringReader(input ?? string.Empty)")
                .Because("Expected nullable string input to be treated as empty input when null.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithNullableDisabledStringInputParameter_GeneratesStringReaderFallback(CancellationToken CancellationToken)
    {
        const string source = """
            #nullable disable
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id is "PT0008" or "PT0003")
                .Because("String input should be accepted even when nullable annotations are disabled.");

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;
            await Assert.That(generatedText)
                .Contains("Run(string input)")
                .Because("Expected generated method signature to preserve the original oblivious string input.");
            await Assert.That(generatedText)
                .Contains("new global::System.IO.StringReader(input ?? string.Empty)")
                .Because("Expected nullable-disabled string input to be treated like unknown nullability and guarded.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTextReaderInputAndTaskReturn_GeneratesAsyncTextReaderPath(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

            await Assert.That(generatedText)
                .Contains("ReadLineAsync(__ct)")
                .Because("Expected TextReader.ReadLineAsync path was not found in generated code.");
            await Assert.That(generatedText)
                .Contains("__pietReadNumberAsyncAwaited")
                .Because("Expected async awaited fallback path for TextReader input was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    public async Task Generator_WithPipeReaderInput_GeneratesSyncPipeReaderPath(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

            await Assert.That(generatedText)
                .Contains("ReadAsync(default(global::System.Threading.CancellationToken)).AsTask().GetAwaiter().GetResult();")
                .Because("Expected synchronous PipeReader.ReadAsync bridge path was not found in generated code.");
            await Assert.That(generatedText)
                .Contains("global::System.Buffers.BuffersExtensions.ToArray(__buffer)")
                .Because("Expected PipeReader number-read conversion path was not found in generated code.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithPipeReaderInputAndTaskReturn_GeneratesAsyncPipeReaderPath(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

            await Assert.That(generatedText)
                .Contains("async global::System.Threading.Tasks.ValueTask<int?> __pietReadNumberAsync")
                .Because("Expected async PipeReader number-read delegate was not found in generated code.");
            await Assert.That(generatedText)
                .Contains("await input.ReadAsync(__ct).ConfigureAwait(false);")
                .Because("Expected PipeReader.ReadAsync await path was not found in generated code.");
            await Assert.That(generatedText)
                .Contains("global::System.Buffers.BuffersExtensions.ToArray(__buffer)")
                .Because("Expected PipeReader buffer conversion path was not found in generated code.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithOutputImage_AndTextWriterParameter_DoesNotReportMissingOutputDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .DoesNotContain(static x => x.Id == "PT0007")
            .Because("PT0007 should not be reported when the method has a TextWriter parameter.");
    }

    [Test]
    public async Task Generator_WithUnusedInputParameter_ReportsHiddenDiagnostic_AndGeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await Assert.That(generatorDiagnostics)
                .Contains(static x => x.Id == "PT0010" && x.Severity == DiagnosticSeverity.Hidden)
                .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run(global::System.IO.Pipelines.PipeReader input)"))
                .Because("Expected generated method implementation was not found.");

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTaskStringReturn_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generated = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .Single(static text => text.Contains("partial class Sample"));


            await Assert.That(generated)
                .Contains("public async partial global::System.Threading.Tasks.Task<string> Run(")
                .Because("Expected async Task<string> signature was not found.");

            await Assert.That(generated)
                .Contains("PietRuntime.ExecuteAsync(")
                .Because("Expected async runtime call was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithIEnumerableByteReturn_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("yield return"))
                .Because("Expected yield return byte path was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithCancellationTokenParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("CancellationToken ct"))
                .Because("Expected CancellationToken parameter was not found in generated code.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    public async Task Generator_WithoutGeneratePietMethodAttribute_DoesNotEmitMethodSource(CancellationToken CancellationToken)
    {
        const string source = "public class Sample { public void Run() { } }";

        var driver = RunGeneratorsAndUpdateCompilation(source, out var outputCompilation, out var diagnostics, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatedHints = runResult.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.HintName).ToArray();

        await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

        await Assert.That(generatedHints)
            .DoesNotContain(MethodGenerator.GeneratedMethodsFileName, StringComparer.Ordinal)
            .DoesNotContain(MethodGenerator.GeneratePietRuntimeFileName, StringComparer.Ordinal);
    }

    [Test]
    public async Task Generator_WithInvalidReturnType_ReportsDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id is "PT0002")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithIntReturn_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial int Run()"))
                .Because("Expected generated int return method implementation was not found.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithIntExitCodePatterns_ReturnsZero(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);


            var generatedText = string.Join("\n", runResult.GeneratedTrees.Select(tree => tree.GetText(CancellationToken).ToString()));

            await Assert.That(generatedText)
                .Contains("RunInt(").Because("Expected RunInt method was not found.");
            await Assert.That(generatedText)
                .Contains("RunTaskInt(").Because("Expected RunTaskInt method was not found.");
            await Assert.That(generatedText)
                .Contains("RunValueTaskInt(").Because("Expected RunValueTaskInt method was not found.");
            await Assert.That(generatedText)
                .Contains("return 0;").Because("Expected exit-code return statement was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithGenericMethod_ReportsDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0003")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithRefParameter_ReportsDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0003")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithDuplicateCancellationToken_ReportsDiagnostic()
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

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0004")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithStringAndPipeReader_ReportsDuplicateParameterDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0003")
            .Because("PT0003 should be reported for multiple input kinds.");
    }

    [Test]
    public async Task Generator_WithDuplicatePipeWriter_ReportsDuplicateParameterDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0004")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithDuplicateTextWriter_ReportsDuplicateParameterDiagnostic(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0004")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithGlobalNamespaceAndStaticMethod_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("program.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedMethod = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("partial class Sample")) ?? string.Empty;

            await Assert.That(generatedMethod)
                .Contains("public static partial")
                .Because("Expected static modifier was not found.");
            // The runtime is appended after the method code in the combined file; extract only the method section.
            const string runtimeSeparator = "\nnamespace Esolang.Piet.__Generated";
            var methodSection = generatedMethod.Contains(runtimeSeparator)
                ? generatedMethod[..generatedMethod.IndexOf(runtimeSeparator, StringComparison.Ordinal)]
                : generatedMethod;
            await Assert.That(methodSection)
                .DoesNotContain("namespace ")
                .Because("Global namespace method should not emit a namespace declaration.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }

    }

    [Test]
    public async Task Generator_WithUnreadableTransformedText_ReportsImageNotFoundDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", null),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0005")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithTransformedTextWithoutPrefix_ReportsImageNotFoundDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0005")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithTransformedTextWithEmptyImagePath_ReportsImageNotFoundDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0005")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithTransformedTextWithoutNewline_ReportsDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0006")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithTransformedTextWithEmptyPayload_ReportsDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0006")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithVerticalOutputTransition_ReportsMissingOutputDiagnostic(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("vertical-output.png")]
                public partial void Run();
            }
            """;

        var verticalOutputPng = BuildStoredRgbPng(1, 2,
        [
            0x00, 0xFF, 0xC0, 0xC0,
            0x00, 0xFF, 0x00, 0xFF,
        ]);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out _,
            out _,
            new TestAdditionalText("obj/vertical-output.png.piet.txt",
                MakeTransformedText("vertical-output.png", verticalOutputPng)),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0007")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithBlackPixelProgram_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("black.png", blackPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public partial void Run()"))
                .Because("Expected generated method implementation was not found.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithCSharp12_ParseOptions_CompilesGeneratedCode(CancellationToken CancellationToken)
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

        _ = RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out var outputCompilation,
            out var diagnostics,
            LanguageVersion.CSharp12,
            [
                new TestAdditionalText("obj/black.png.piet.txt",
                MakeTransformedText("black.png", blackPng))
            ],
            CancellationToken: CancellationToken);
        try
        {
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var diagnostics2 = outputCompilation.GetDiagnostics(CancellationToken);
            await Assert.That(diagnostics2)
                .DoesNotContain(static x => x.Severity == DiagnosticSeverity.Error)
                .Because("Compilation contains errors after running generator with C#12 parse options.\r\n"
                + string.Join("\r\n", diagnostics2.Select(v => v.ToString())));
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithLanguageVersionBelowCSharp8_ReportsLanguageVersionWarning(CancellationToken CancellationToken)
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

        var driver = RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out _,
            out _,
            LanguageVersion.CSharp7_3,
            [
                new TestAdditionalText("obj/black.png.piet.txt",
                MakeTransformedText("black.png", blackPng))
            ],
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0012" && x.Severity == DiagnosticSeverity.Warning)
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    public async Task Generator_WithValueTaskStringReturn_GeneratesMethod(CancellationToken CancellationToken)
    {
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("public async partial global::System.Threading.Tasks.ValueTask<string> Run()"));
            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("return __pietString;"));
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithIAsyncEnumerableByteReturn_GeneratesAsyncMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var generatedText = runResult.GeneratedTrees
                .Select(tree => tree.GetText(CancellationToken).ToString())
                .FirstOrDefault(static t => t.Contains("IAsyncEnumerable")) ?? string.Empty;

            await Assert.That(generatedText)
                .Contains("public async partial global::System.Collections.Generic.IAsyncEnumerable<byte> Run([global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken ct)")
                .Because("Expected async modifier in generated code.");
            await Assert.That(generatedText)
                .Contains("yield return __pietByte;")
                .Because("Expected yield return byte path was not found.");

        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithPipeReaderInput_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            var pipeReaderErrors = outputCompilation.GetDiagnostics(CancellationToken)
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())
                .ToArray();

            await Assert.That(pipeReaderErrors)
                .IsEmpty()
                .Because("Compilation contains errors after running generator. " + string.Join(" | ", pipeReaderErrors));
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithPipeWriterOutput_GeneratesMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(runResult.GeneratedTrees)
                .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("global::System.Buffers.BuffersExtensions.Write(output, "))
                .Because("Expected PipeWriter.WriteAsync() was not found in generated code.");

            var pipeWriterErrors = outputCompilation.GetDiagnostics(CancellationToken)
                .Where(static x => x.Severity == DiagnosticSeverity.Error)
                .Select(static x => x.ToString())
                .ToArray();

            await Assert.That(pipeWriterErrors)
                .IsEmpty()
                .Because("Compilation contains errors after running generator. " + string.Join(" | ", pipeWriterErrors));
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    [Arguments("hello-world.png", "samples", "Generator.UseConsole", "samples", "hello-world.png", "Piet")]
    [Arguments("ascii-piet-sample.txt", "samples", "Generator.UseConsole", "samples", "ascii-piet-sample.txt", "Piet")]
    [Arguments("ppm-sample.ppm", "samples", "Generator.UseConsole", "samples", "ppm-sample.ppm", "Piet")]
    [Arguments("dot.gif", "samples", "Generator.UseConsole", "samples", "dot.gif", "Piet")]
    [Arguments("dot-codel-11.gif", "samples", "Generator.UseConsole", "samples", "dot-codel-11.gif", "Piet")]
    [Arguments("dot.appp", "samples", "Generator.UseConsole", "samples", "dot.appp", "PietPlusPlus")]
    public async Task Generator_WithSampleConformanceVector_GeneratesWithoutDiagnostics(
        string logicalPath,
        string p1,
        string p2,
        string p3,
        string p4,
        string language,
        CancellationToken CancellationToken)
    {
        var samplePath = FindFileInRepository(p1, p2, p3, p4);
        var transformed = MakeTransformedText(logicalPath,
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
        File.ReadAllBytes(samplePath)
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない
        , language);
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
            new TestAdditionalText($"obj/{logicalPath}.piet.txt", transformed),
            CancellationToken: CancellationToken);

        await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

    }

    [Test]
    public async Task TryGetLanguageInt_WithPietPlusPlusHeader_Returns1()
    {
        var text = "// PIET_IMAGE_PATH=dot.appp\n// PIET_CODEL_SIZE=1\n// PIET_LANGUAGE=PietPlusPlus\nfnw=";
        var result = MethodGenerator.TryGetLanguageInt(text);
        await Assert.That(result)
            .IsEqualTo(1)
            .Because($"Expected PietPlusPlus (1) but got {result?.ToString() ?? "null"}");
    }

    [Test]
    public async Task TryGetLanguageInt_WithPietHeader_Returns0()
    {
        var text = "// PIET_IMAGE_PATH=hello.png\n// PIET_CODEL_SIZE=1\n// PIET_LANGUAGE=Piet\nabc=";
        var result = MethodGenerator.TryGetLanguageInt(text);
        await Assert.That(result)
            .IsEqualTo(0)
            .Because($"Expected Piet (0) but got {result?.ToString() ?? "null"}");
    }

    [Test]
    public async Task Generator_WithDotApppAndExplicitLanguageAttribute_GeneratesWithoutDiagnostics(CancellationToken CancellationToken)
    {
        var samplePath = FindFileInRepository("samples", "Generator.UseConsole", "samples", "dot.appp");
        var transformed = MakeTransformedText("dot.appp",
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
            File.ReadAllBytes(samplePath)
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない
            , "PietPlusPlus");
        var source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("dot.appp", language: (Esolang.Piet.LanguageType)1)]
                public static partial void RunDot();
            }
            """;

        _ = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/dot.appp.piet.txt", transformed),
            CancellationToken: CancellationToken);

        await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);
    }

    [Test]
    public async Task Generator_WithDotApppHeaderLanguage_GeneratesWithPietPlusPlusRuntime(CancellationToken CancellationToken)
    {
        var samplePath = FindFileInRepository("samples", "Generator.UseConsole", "samples", "dot.appp");
        var transformed = MakeTransformedText("dot.appp",
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
                File.ReadAllBytes(samplePath),
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない
                "PietPlusPlus");
        var source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("dot.appp")]
                public static partial void RunDot();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/dot.appp.piet.txt", transformed),
            CancellationToken: CancellationToken);

        var generatedTrees = driver.GetRunResult().GeneratedTrees;
        var generatedSource = generatedTrees.Length > 0 ? string.Concat(generatedTrees.Select(t => t.ToString())) : "(no source generated)";
        var errorCount = 0;
        foreach (var d in diagnostics)
            if (d.Severity == DiagnosticSeverity.Error)
                errorCount++;

        await Assert.That(errorCount)
            .IsEqualTo(0)
            .Because($"Diagnostics: {string.Join(", ", diagnostics.Select(d => $"[{d.Id}: {d.GetMessage()}]"))}\nGenerated:\n{generatedSource}");

        var hasPietPlusPlus = generatedSource.IndexOf("PietPlusPlus", StringComparison.Ordinal) >= 0;
        if (!hasPietPlusPlus) throw new AssertionException($"Expected PietPlusPlusRuntime usage, but got:\n{generatedSource}");
    }

    [Test]
    public async Task Generator_WithInputOutputSampleAndNoInputMechanism_ReportsDiagnostic(CancellationToken CancellationToken)
    {
        const string logicalPath = "input-output.png";
        var samplePath = FindFileInRepository("samples", "Generator.UseConsole", "samples", logicalPath);
        var transformed = MakeTransformedText(logicalPath,
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
            File.ReadAllBytes(samplePath)
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない
        );
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
            new TestAdditionalText("obj/input-output.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0008")
            .Because(string.Join("\n", generatorDiagnostics.Select(static x => x.ToString())));
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithLoggerParameter_LogsExecution(CancellationToken CancellationToken)
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
                [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=1,nqiaecfbknmeRtakcsqimemerTcnjlvutqiavtFsvubntqcslnqBbjemu   udjiNqifu  r  tlaFvldq rrr vneVujbm  k  nmsRsnadv a vfkqSkdceumbmuqcrVqesqfnbsrvtjUnfcltltdljceMkcltuemnbfnkB")]
                public partial string Run(FakeLogger logger, System.Threading.CancellationToken ct);
            }
            """;

        var transformed = MakeTransformedText("program.png", TwoPixelOutputPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);


            var runResult = driver.GetRunResult();
            await AssertNoErrors(runResult.Diagnostics, CancellationToken: CancellationToken);

            var asm = await Emit(outputCompilation, CancellationToken);
            var t = asm.GetType("Demo.Sample")!;
            var instance = Activator.CreateInstance(t)!;
            var typelogger = asm.GetType("Demo.FakeLogger");
            Assert.NotNull(typelogger);
            var logger = Activator.CreateInstance(typelogger);
            Assert.NotNull(logger);
            var logs = typelogger.GetField("Logs")?.GetValue(logger) as List<string>;
            Assert.NotNull(logs);

            var m = t.GetMethod("Run");
            Assert.NotNull(m);
            var result = m.Invoke(instance, [logger, CancellationToken]) as string;
            LogWriteLine($"result: {result}");
            await Assert.That(logs)
                .IsNotEmpty()
                .Because("No logs were captured.");
            LogWriteLine($"logs: {string.Join("\n", logs)}");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithLoggerPrimaryConstructor_LogsExecution(CancellationToken CancellationToken)
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
                [Esolang.Piet.GeneratePietMethod("data:text/ascii-piet;codel-size=1,nqiaecfbknmeRtakcsqimemerTcnjlvutqiavtFsvubntqcslnqBbjemu   udjiNqifu  r  tlaFvldq rrr vneVujbm  k  nmsRsnadv a vfkqSkdceumbmuqcrVqesqfnbsrvtjUnfcltltdljceMkcltuemnbfnkB")]
                public partial string Run(System.Threading.CancellationToken ct);
            }
            """;

        var transformed = MakeTransformedText("program.png", TwoPixelOutputPng);
        var driver = RunGeneratorsAndUpdateCompilation(
            source,
            out var outputCompilation,
            out var diagnostics,
            new TestAdditionalText("obj/program.png.piet.txt", transformed),
            CancellationToken: CancellationToken);

        try
        {
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);


            var runResult = driver.GetRunResult();

            await AssertNoErrors(runResult.Diagnostics, CancellationToken: CancellationToken);

            var asm = await Emit(outputCompilation, CancellationToken);
            var t = asm.GetType("Demo.Sample")!;
            var typelogger = asm.GetType("Demo.FakeLogger");
            Assert.NotNull(typelogger);
            var logger = Activator.CreateInstance(typelogger);
            Assert.NotNull(logger);
            var logs = typelogger.GetField("Logs")?.GetValue(logger) as List<string>;
            Assert.NotNull(logs);
            var instance = Activator.CreateInstance(t, [logger]);
            Assert.NotNull(instance);

            var m = t.GetMethod("Run");
            Assert.NotNull(m);
            var reuslt = m.Invoke(instance, [CancellationToken]) as string;
            Assert.NotNull(reuslt);
            LogWriteLine($"result: {reuslt}");
            await Assert.That(logs)
                .IsNotEmpty()
                .Because("No logs were captured.");
            LogWriteLine($"logs: {string.Join("\n", logs)}");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }



    async Task<Assembly> Emit(Compilation compilation, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms, cancellationToken: cancellationToken);
        await Assert.That(result.Success)
            .IsTrue()
            .Because("Compilation emit failed");
        ms.Seek(0, SeekOrigin.Begin);

#if NET
        var ctx = new System.Runtime.Loader.AssemblyLoadContext(nameof(MethodGeneratorTests), isCollectible: true);
        return ctx.LoadFromStream(ms);
#else
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
        return System.Reflection.Assembly.Load(ms.ToArray());
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない
#endif
    }
    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        AdditionalText additionalTexts,
        LanguageVersion? languageVersion = null,
        CancellationToken CancellationToken = default)
        => RunGeneratorsAndUpdateCompilation(
            source,
            out outputCompilation,
            out diagnostics,
            [additionalTexts],
            languageVersion,
            CancellationToken);
    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        AdditionalText[]? additionalTexts = null,
        LanguageVersion? languageVersion = null,
        CancellationToken CancellationToken = default)
        => RunGeneratorsAndUpdateCompilationWithLanguageVersion(
            source,
            out outputCompilation,
            out diagnostics,
            languageVersion ?? LanguageVersion.CSharp12,
            additionalTexts ?? [],
            CancellationToken);

    GeneratorDriver RunGeneratorsAndUpdateCompilationWithLanguageVersion(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        LanguageVersion languageVersion,
        AdditionalText[] additionalTexts,
        CancellationToken CancellationToken)
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
#pragma warning disable RS1035 // アナライザーに対して禁止された API を使用しない
            if (File.Exists(candidate))
                return candidate;
#pragma warning restore RS1035 // アナライザーに対して禁止された API を使用しない

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find file in repository: {Path.Combine(relativeParts)}");
    }

    [Test]
    public async Task Generator_WithCodelSizeAttribute_OverridesAdditionalFile(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/program.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            await Assert.That(runResult.Diagnostics)
                .IsEmpty()
                .Because(string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            // 生成コードにcodelSize=2が反映されているか（例: 配列長やコメント等で判定）
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            await Assert.That(generated)
                .IsNotNull()
                .Because("Method not generated");
            await Assert.That(generated)
                .Contains("codelSize: 2")
                .Because("codelSize=2 not reflected in generated code");
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithCodelSizeFromAdditionalFile_UsedWhenNoAttribute(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/program.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            await Assert.That(runResult.Diagnostics)
                .IsEmpty()
                .Because(string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            await Assert.That(generated)
                .IsNotNull()
                .Because("Method not generated");
            await Assert.That(generated)
                .Contains("codelSize: 2")
                .Because("codelSize=2 not reflected in generated code");
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithInlineAsciiPiet_UsedAttribute(CancellationToken CancellationToken)
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
            out var diagnostics,
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            await Assert.That(runResult.Diagnostics)
                .IsEmpty()
                .Because(string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));
            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            await Assert.That(generated)
                .IsNotNull()
                .Because("Method not generated");
            await Assert.That(generated)
                .Contains("codelSize: 1")
                .Because("codelSize=1 not reflected in generated code");
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithInlineAsciiPietBase64DataUri_UsedAttribute(CancellationToken CancellationToken)
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
            out var diagnostics,
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            await Assert.That(runResult.Diagnostics)
                .IsEmpty()
                .Because(string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));

            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            await Assert.That(generated)
                .IsNotNull()
                .Because("Method not generated");
            await Assert.That(generated)
                .Contains("codelSize: 1")
                .Because("codelSize=1 not reflected in generated code");
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithInlineAsciiPietInvalidCodelSize_FallsBackToDefault(CancellationToken CancellationToken)
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
            out var diagnostics,
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            await Assert.That(runResult.Diagnostics)
                .IsEmpty()
                .Because(string.Join("\n", runResult.Diagnostics.Select(static x => x.ToString())));

            var generated = runResult.GeneratedTrees.Select(t => t.GetText(CancellationToken).ToString()).FirstOrDefault(x => x.Contains("partial void Run"));
            await Assert.That(generated)
                .IsNotNull()
                .Because("Method not generated");
            await Assert.That(generated)
                .Contains("codelSize: 1")
                .Because("codelSize fallback to default(1) was not reflected in generated code");
            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithInlineDataUriMissingPayloadSeparator_ReportsImageNotFound(CancellationToken CancellationToken)
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

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics).Contains(x => x.Id == "PT0005");
        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0005"))
            .Because("Expected throw for PT0005 was not generated.");
    }

    [Test]
    public async Task Generator_WithInvalidImagePath_GeneratesThrowingMethod(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();

        // PT0001 が出る
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
        await Assert.That(generatorDiagnostics).Contains(x => x.Id == "PT0001");

        // 生成コードに throw が含まれる
        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0001"))
            .Because("Expected throw for PT0001 was not generated.");
    }

    [Test]
    public async Task Generator_WithMissingImageFile_GeneratesThrowingMethod(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public partial class Sample
            {
                [Esolang.Piet.GeneratePietMethod("missing.png")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out _, out _, CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();

        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
        await Assert.That(generatorDiagnostics).Contains(static x => x.Id == "PT0005");

        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0005"))
            .Because("Expected throw for PT0005 was not generated.");
    }

    [Test]
    public async Task Generator_WithInvalidImageFormat_GeneratesThrowingMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("invalid.png", null),
            CancellationToken: CancellationToken);

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics).Contains(static x => x.Id == "PT0006");

        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.NotImplementedException(\"PT0006"))
            .Because("Expected throw for PT0006 was not generated.");
    }

    [Test]
    public async Task Generator_WithOutputImage_AndNoOutputMechanism_GeneratesThrowingMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("output.png", TwoPixelOutputPng)),
            CancellationToken: CancellationToken);

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics).Contains(static x => x.Id == "PT0007");

        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.InvalidOperationException(\"PT0007"))
            .Because("Expected throw for PT0007 was not generated.");
    }

    [Test]
    public async Task Generator_WithInputImage_AndNoInputMechanism_GeneratesThrowingMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("input.png", TwoPixelInputPng)),
            CancellationToken: CancellationToken);

        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics).Contains(static x => x.Id == "PT0008");

        await Assert.That(runResult.GeneratedTrees)
            .Contains(tree => tree.GetText(CancellationToken).ToString().Contains("throw new global::System.InvalidOperationException(\"PT0008"))
            .Because("Expected throw for PT0008 was not generated.");
    }

    [Test]
    public async Task Generator_WithTaskReturnAndTextWriterParameter_GeneratesCorrectMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task return + TextWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTaskReturnAndPipeWriterParameter_GeneratesCorrectMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task return + PipeWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithValueTaskReturnAndTextWriterParameter_GeneratesCorrectMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask return + TextWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generator_WithValueTaskReturnAndPipeWriterParameter_GeneratesCorrectMethod(CancellationToken CancellationToken)
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
                MakeTransformedText("samples/hello-world.png", MinimalLightRedPng)),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask return + PipeWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    static async Task AssertNoErrors(ImmutableArray<Diagnostic> diagnostics, Compilation? compilation = null, CancellationToken CancellationToken = default)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        var errorDetails = string.Join("; ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
        await Assert.That(errors).IsEmpty().Because($"{errors.Length} error(s) in generator output: [{errorDetails}]");
        var errors2 = compilation?.GetDiagnostics(CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors2 ?? []).IsEmpty().Because($"{errors2?.Length ?? 0} error(s) in compilation");
    }

    // -----------------------------------------------------------------------
    // int / Task<int> / ValueTask<int> + TextWriter / PipeWriter の組み合わせ
    // -----------------------------------------------------------------------

    [Test]
    public async Task Generator_WithIntReturnAndTextWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);

        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for int return + TextWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithIntReturnAndPipeWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for int return + PipeWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTaskIntReturnAndTextWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task<int> return + TextWriter parameter.");

        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithTaskIntReturnAndPipeWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for Task<int> return + PipeWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithValueTaskIntReturnAndTextWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask<int> return + TextWriter parameter.");


        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithValueTaskIntReturnAndPipeWriterParameter_GeneratesMethod(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

            await AssertNoErrors(diagnostics, outputCompilation, CancellationToken);

            await Assert.That(generatorDiagnostics)
                .DoesNotContain(static x => x.Id == "PT0011")
                .Because("PT0011 should not be reported for ValueTask<int> return + PipeWriter parameter.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generator_WithStringReturnAndPipeWriterParameter_ReportsReturnOutputConflictDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0011")
            .Because("PT0011 should be reported for string return + PipeWriter parameter.");
    }

    [Test]
    public async Task Generator_WithIEnumerableByteReturnAndTextWriterParameter_ReportsReturnOutputConflictDiagnostic(CancellationToken CancellationToken)
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
            new TestAdditionalText("obj/hello-world.png.piet.txt", transformed),
            CancellationToken: CancellationToken);
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results.SelectMany(static r => r.Diagnostics).ToImmutableArray();

        await Assert.That(generatorDiagnostics)
            .Contains(static x => x.Id == "PT0011")
            .Because("PT0011 should be reported for IEnumerable<byte> return + TextWriter parameter.");
    }
}
