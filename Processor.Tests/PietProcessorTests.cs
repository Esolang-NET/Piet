using Esolang.Piet.Parser;
using Esolang.Processor;
using System.Reflection;
using TUnit.Assertions.Enums;
using static Esolang.Processor.IOEvent;

namespace Esolang.Piet.Processor.Tests;

public sealed class PietProcessorTests
{
    public TestContext TestContext { get; set; } = default!;

    static readonly MethodInfo ExecuteCommandMethod = typeof(PietProcessor)
        .GetMethod("ExecuteCommand", BindingFlags.NonPublic | BindingFlags.Static)!;

    static readonly MethodInfo ApplyRetryMethod = typeof(PietProcessor)
        .GetMethod("ApplyRetry", BindingFlags.NonPublic | BindingFlags.Static)!;

    static readonly MethodInfo SlideWhiteMethod = typeof(PietProcessor)
        .GetMethod("SlideWhite", BindingFlags.NonPublic | BindingFlags.Static)!;

    static readonly MethodInfo FindEdgeMethod = typeof(PietProcessor)
        .GetMethod("FindEdge", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Test]
    public async Task Constructor_StoresProgram()
    {
        var program = new PietProgram(1, 1, [PietColor.White]);

        var processor = new PietProcessor(program);

        await Assert.That(program).IsSameReferenceAs(((IProcessor<PietProgram>)processor).Program);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Run_StopsWhenSlideWhiteCannotProceed(CancellationToken CancellationToken)
    {
        var program = new PietProgram(
            2,
            1,
            [
                PietColor.LightRed,
                PietColor.White,
            ]);

        using var output = new StringWriter();
        var processor = new PietProcessor(program);

        await RunToEndManualAsync(processor, null, output, CancellationToken).ConfigureAwait(false);

        await Assert.That(output.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Run_ExecutesNoOpProgramWithoutOutput(CancellationToken CancellationToken)
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        using var output = new StringWriter();
        var processor = new PietProcessor(program);

        await RunToEndManualAsync(processor, null, output, CancellationToken).ConfigureAwait(false);

        await Assert.That(output.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunAndOutputString_ReturnsNullWhenNoOutputIsProduced(CancellationToken CancellationToken)
    {
        var processor = new PietProcessor(new PietProgram(1, 1, [PietColor.White]));

        var result = await RunAndOutputStringAsync(processor, cancellationToken: CancellationToken).ConfigureAwait(false);

        await Assert.That(result).IsNull();
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunAndOutputString_ParsesAndRunsHelloWorldSample(CancellationToken CancellationToken)
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", "hello-world.png");
        var program = PietParser.Parse(path, cancellationToken: CancellationToken);
        var processor = new PietProcessor(program);

        var result = await RunAndOutputStringAsync(processor, cancellationToken: CancellationToken).ConfigureAwait(false);

        await Assert.That(result).IsEqualTo("Hello, world!");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunToEnd_ReturnsZero(CancellationToken CancellationToken)
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        var processor = new PietProcessor(program);

        var exitCode = await RunToEndManualAsync(processor, null, null, CancellationToken).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunToEndAsync_ReturnsZero(CancellationToken CancellationToken)
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        var processor = new PietProcessor(program);

        var exitCode = await RunToEndManualAsync(processor, null, null, CancellationToken).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    static async Task<string?> RunAndOutputStringAsync(PietProcessor processor, TextReader? input = null, CancellationToken cancellationToken = default)
    {
        using var writer = new StringWriter();
        await RunToEndManualAsync(processor, input, writer, cancellationToken).ConfigureAwait(false);
        var result = writer.ToString().TrimEnd('\0', '\r', '\n');
        return result.Length == 0 ? null : result;
    }

    static async Task<int> RunToEndManualAsync(PietProcessor processor, TextReader? input, TextWriter? output, CancellationToken cancellationToken)
    {
        await foreach (var ioEvent in processor.RunAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            switch (ioEvent)
            {
                case InputCharEvent charInput:
                    if (input != null)
                    {
                        var buffer = new char[1];
                        if (await input.ReadAsync(buffer, 0, 1).ConfigureAwait(false) > 0)
                            charInput.Write(buffer[0]);
                    }
                    break;
                case InputIntEvent intInput:
                    if (input != null)
                    {
                        var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (int.TryParse(line, out var i))
                            intInput.Write(i);
                    }
                    break;
                case OutputCharEvent charOutput:
                    if (output != null)
                        await output.WriteAsync(charOutput.Output).ConfigureAwait(false);
                    break;
                case OutputIntEvent intOutput:
                    if (output != null)
                        await output.WriteLineAsync(intOutput.Output.ToString()).ConfigureAwait(false);
                    break;
                case EndEvent endEvent:
                    return endEvent.ExitCode;
            }
        }
        return 0;
    }

    [Test]
    public async Task ExecuteCommand_CoversArithmeticFlowAndIoCommands()
    {
        var stack = new List<int>();
        var dp = 0;
        var cc = 0;

        InvokeExecuteCommand(1, 7, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[7], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([2, 3]);
        InvokeExecuteCommand(3, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[5], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([9, 4]);
        InvokeExecuteCommand(4, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[5], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([3, 4]);
        InvokeExecuteCommand(5, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[12], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([8, 2]);
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[4], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([8, 0]);
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[8, 0], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([8, 3]);
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[2], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([8, 0]);
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[8, 0], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.Add(0);
        InvokeExecuteCommand(8, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[1], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([5, 3]);
        InvokeExecuteCommand(9, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[1], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.Add(-1);
        dp = 0;
        InvokeExecuteCommand(10, 0, stack, ref dp, ref cc);
        await Assert.That(dp).IsEqualTo(3);

        stack.Clear();
        stack.Add(3);
        cc = 0;
        InvokeExecuteCommand(11, 0, stack, ref dp, ref cc);
        await Assert.That(cc).IsEqualTo(1);

        stack.Clear();
        stack.Add(42);
        InvokeExecuteCommand(12, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[42, 42], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([1, 2, 3, 3, 1]);
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[3, 1, 2], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.AddRange([9, 0, 0]);
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEquivalentTo((int[])[9], ordering: CollectionOrdering.Matching);

        stack.Clear();
        stack.Add(1);
        InvokeExecuteCommand(2, 0, stack, ref dp, ref cc);
        await Assert.That(stack).IsEmpty();
    }

    [Test]
    public async Task PrivateHelpers_CoverRetrySlideAndEdgeSelection()
    {
        var dp = 0;
        var cc = 0;
        var retryArgs = new object[] { 0, dp, cc };
        ApplyRetryMethod.Invoke(null, retryArgs);
        dp = (int)retryArgs[1];
        cc = (int)retryArgs[2];
        await Assert.That(cc).IsEqualTo(1);

        retryArgs = [1, dp, cc];
        ApplyRetryMethod.Invoke(null, retryArgs);
        dp = (int)retryArgs[1];
        await Assert.That(dp).IsEqualTo(1);

        var codels = new[] { PietColor.White, PietColor.Red };
        var slideArgs = new object[] { codels, 2, 1, 0, 0, 0 };
        var slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        await Assert.That(slideResult).IsTrue();
        await Assert.That((int)slideArgs[3]).IsEqualTo(1);

        var deadEnd = new[] { PietColor.White, PietColor.White };
        slideArgs = [deadEnd, 2, 1, 0, 0, 0];
        slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        await Assert.That(slideResult).IsFalse();

        var block = new List<(int x, int y)> { (0, 0), (2, 0), (1, 1) };
        var edge0 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 0, 0])!;
        var edge1 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 1, 1])!;
        var edge2 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 2, 0])!;
        var edge3 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 3, 1])!;

        await Assert.That(edge0).IsEqualTo((2, 0));
        await Assert.That(edge1).IsEqualTo((1, 1));
        await Assert.That(edge2).IsEqualTo((0, 0));
        await Assert.That(edge3).IsEqualTo((2, 0));
    }

    static void InvokeExecuteCommand(int commandIndex, int blockSize, List<int> stack,
        ref int dp, ref int cc)
    {
        var hDiff = commandIndex / 3;
        var lDiff = commandIndex % 3;
        var args = new object[] { hDiff, lDiff, blockSize, stack, dp, cc };
        ExecuteCommandMethod.Invoke(null, args);
        dp = (int)args[4];
        cc = (int)args[5];
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
}
