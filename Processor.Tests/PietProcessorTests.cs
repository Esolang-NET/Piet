using Esolang.Piet.Parser;
using System.Reflection;

namespace Esolang.Piet.Processor.Tests;

[TestClass]
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

    [TestMethod]
    public void Constructor_StoresProgram()
    {
        var program = new PietProgram(1, 1, [PietColor.White]);

        var processor = new PietProcessor(program);

        Assert.AreSame(program, processor.Program);
    }

    [TestMethod]
    public void Run_StopsWhenSlideWhiteCannotProceed()
    {
        var program = new PietProgram(
            2,
            1,
            [
                PietColor.LightRed,
                PietColor.White,
            ]);

        using var output = new StringWriter();
        var processor = new PietProcessor(program, output);

        processor.Run();

        Assert.AreEqual(string.Empty, output.ToString());
    }

    [TestMethod]
    public void Run_ExecutesNoOpProgramWithoutOutput()
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        using var output = new StringWriter();
        var processor = new PietProcessor(program, output);

        processor.Run();

        Assert.AreEqual(string.Empty, output.ToString());
    }

    [TestMethod]
    public void RunAndOutputString_ReturnsNullWhenNoOutputIsProduced()
    {
        var processor = new PietProcessor(new PietProgram(1, 1, [PietColor.White]));

        var result = processor.RunAndOutputString();

        Assert.IsNull(result);
    }

    [TestMethod]
    public void RunAndOutputString_ParsesAndRunsHelloWorldSample()
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", "hello-world.png");
        var program = PietParser.Parse(path);
        var processor = new PietProcessor(program);

        var result = processor.RunAndOutputString();

        Assert.AreEqual("Hello, world!", result);
    }

    [TestMethod]
    public void RunToEnd_ReturnsZero()
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        var processor = new PietProcessor(program);

        var exitCode = processor.RunToEnd(cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunToEndAsync_ReturnsZero()
    {
        var program = new PietProgram(1, 1, [PietColor.White]);
        var processor = new PietProcessor(program);

        var exitCode = await processor.RunToEndAsync(cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void ExecuteCommand_CoversArithmeticFlowAndIoCommands()
    {
        var stack = new List<int>();
        var dp = 0;
        var cc = 0;

        InvokeExecuteCommand(1, 7, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 7 }, stack);

        stack.Clear();
        stack.AddRange([2, 3]);
        InvokeExecuteCommand(3, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 5 }, stack);

        stack.Clear();
        stack.AddRange([9, 4]);
        InvokeExecuteCommand(4, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 5 }, stack);

        stack.Clear();
        stack.AddRange([3, 4]);
        InvokeExecuteCommand(5, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 12 }, stack);

        stack.Clear();
        stack.AddRange([8, 2]);
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 4 }, stack);

        stack.Clear();
        stack.AddRange([8, 0]);
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 8, 0 }, stack);

        stack.Clear();
        stack.AddRange([8, 3]);
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 2 }, stack);

        stack.Clear();
        stack.AddRange([8, 0]);
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 8, 0 }, stack);

        stack.Clear();
        stack.Add(0);
        InvokeExecuteCommand(8, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 1 }, stack);

        stack.Clear();
        stack.AddRange([5, 3]);
        InvokeExecuteCommand(9, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 1 }, stack);

        stack.Clear();
        stack.Add(-1);
        dp = 0;
        InvokeExecuteCommand(10, 0, stack, ref dp, ref cc);
        Assert.AreEqual(3, dp);

        stack.Clear();
        stack.Add(3);
        cc = 0;
        InvokeExecuteCommand(11, 0, stack, ref dp, ref cc);
        Assert.AreEqual(1, cc);

        stack.Clear();
        stack.Add(42);
        InvokeExecuteCommand(12, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 42, 42 }, stack);

        stack.Clear();
        stack.AddRange([1, 2, 3, 3, 1]);
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, stack);

        stack.Clear();
        stack.AddRange([9, 0, 0]);
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc);
        CollectionAssert.AreEqual(new[] { 9 }, stack);

        stack.Clear();
        stack.Add(1);
        InvokeExecuteCommand(2, 0, stack, ref dp, ref cc);
        Assert.IsEmpty(stack);
    }

    [TestMethod]
    public void PrivateHelpers_CoverRetrySlideAndEdgeSelection()
    {
        var dp = 0;
        var cc = 0;
        var retryArgs = new object[] { 0, dp, cc };
        ApplyRetryMethod.Invoke(null, retryArgs);
        dp = (int)retryArgs[1];
        cc = (int)retryArgs[2];
        Assert.AreEqual(1, cc);

        retryArgs = [1, dp, cc];
        ApplyRetryMethod.Invoke(null, retryArgs);
        dp = (int)retryArgs[1];
        Assert.AreEqual(1, dp);

        var codels = new[] { PietColor.White, PietColor.Red };
        var slideArgs = new object[] { codels, 2, 1, 0, 0, 0 };
        var slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        Assert.IsTrue(slideResult);
        Assert.AreEqual(1, (int)slideArgs[3]);

        var deadEnd = new[] { PietColor.White, PietColor.White };
        slideArgs = [deadEnd, 2, 1, 0, 0, 0];
        slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        Assert.IsFalse(slideResult);

        var block = new List<(int x, int y)> { (0, 0), (2, 0), (1, 1) };
        var edge0 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 0, 0])!;
        var edge1 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 1, 1])!;
        var edge2 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 2, 0])!;
        var edge3 = ((int x, int y))FindEdgeMethod.Invoke(null, [block, 3, 1])!;

        Assert.AreEqual((2, 0), edge0);
        Assert.AreEqual((1, 1), edge1);
        Assert.AreEqual((0, 0), edge2);
        Assert.AreEqual((2, 0), edge3);
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
