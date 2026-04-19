using Esolang.Piet.Parser;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Esolang.Piet.Processor.Tests;

[TestClass]
public sealed class PietProcessorTests
{
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
        var program = new PietProgram(1, 1, new[] { PietColor.White });

        var processor = new PietProcessor(program);

        Assert.AreSame(program, processor.Program);
    }

    [TestMethod]
    public void Run_StopsWhenSlideWhiteCannotProceed()
    {
        var program = new PietProgram(
            2,
            1,
            new[]
            {
                PietColor.LightRed,
                PietColor.White,
            });

        using var output = new StringWriter();
        var processor = new PietProcessor(program, output);

        processor.Run();

        Assert.AreEqual(string.Empty, output.ToString());
    }

    [TestMethod]
    public void Run_ExecutesNoOpProgramWithoutOutput()
    {
        var program = new PietProgram(1, 1, new[] { PietColor.White });
        using var output = new StringWriter();
        var processor = new PietProcessor(program, output);

        processor.Run();

        Assert.AreEqual(string.Empty, output.ToString());
    }

    [TestMethod]
    public void RunAndOutputString_ReturnsNullWhenNoOutputIsProduced()
    {
        var processor = new PietProcessor(new PietProgram(1, 1, new[] { PietColor.White }));

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
    public void ExecuteCommand_CoversArithmeticFlowAndIoCommands()
    {
        var stack = new List<int>();
        var reader = new StringReader("123\nZ");
        using var writer = new StringWriter();
        var dp = 0;
        var cc = 0;

        InvokeExecuteCommand(1, 7, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 7 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 2, 3 });
        InvokeExecuteCommand(3, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 5 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 9, 4 });
        InvokeExecuteCommand(4, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 5 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 3, 4 });
        InvokeExecuteCommand(5, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 12 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 8, 2 });
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 4 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 8, 0 });
        InvokeExecuteCommand(6, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 8, 0 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 8, 3 });
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 2 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 8, 0 });
        InvokeExecuteCommand(7, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 8, 0 }, stack);

        stack.Clear();
        stack.Add(0);
        InvokeExecuteCommand(8, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 1 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 5, 3 });
        InvokeExecuteCommand(9, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 1 }, stack);

        stack.Clear();
        stack.Add(-1);
        dp = 0;
        InvokeExecuteCommand(10, 0, stack, ref dp, ref cc, reader, writer);
        Assert.AreEqual(3, dp);

        stack.Clear();
        stack.Add(3);
        cc = 0;
        InvokeExecuteCommand(11, 0, stack, ref dp, ref cc, reader, writer);
        Assert.AreEqual(1, cc);

        stack.Clear();
        stack.Add(42);
        InvokeExecuteCommand(12, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 42, 42 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 1, 2, 3, 3, 1 });
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, stack);

        stack.Clear();
        stack.AddRange(new[] { 9, 0, 0 });
        InvokeExecuteCommand(13, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 9 }, stack);

        stack.Clear();
        InvokeExecuteCommand(14, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 123 }, stack);

        InvokeExecuteCommand(15, 0, stack, ref dp, ref cc, reader, writer);
        CollectionAssert.AreEqual(new[] { 123, (int)'Z' }, stack);

        stack.Clear();
        stack.Add(99);
        InvokeExecuteCommand(16, 0, stack, ref dp, ref cc, reader, writer);

        stack.Add((int)'A');
        InvokeExecuteCommand(17, 0, stack, ref dp, ref cc, reader, writer);
        Assert.AreEqual("99A", writer.ToString());

        stack.Clear();
        stack.Add(1);
        InvokeExecuteCommand(2, 0, stack, ref dp, ref cc, reader, writer);
        Assert.AreEqual(0, stack.Count);
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

        retryArgs = new object[] { 1, dp, cc };
        ApplyRetryMethod.Invoke(null, retryArgs);
        dp = (int)retryArgs[1];
        Assert.AreEqual(1, dp);

        var codels = new[] { PietColor.White, PietColor.Red };
        var slideArgs = new object[] { codels, 2, 1, 0, 0, 0 };
        var slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        Assert.IsTrue(slideResult);
        Assert.AreEqual(1, (int)slideArgs[3]);

        var deadEnd = new[] { PietColor.White, PietColor.White };
        slideArgs = new object[] { deadEnd, 2, 1, 0, 0, 0 };
        slideResult = (bool)SlideWhiteMethod.Invoke(null, slideArgs)!;
        Assert.IsFalse(slideResult);

        var block = new List<(int x, int y)> { (0, 0), (2, 0), (1, 1) };
        var edge0 = ((int x, int y))FindEdgeMethod.Invoke(null, new object[] { block, 0, 0 })!;
        var edge1 = ((int x, int y))FindEdgeMethod.Invoke(null, new object[] { block, 1, 1 })!;
        var edge2 = ((int x, int y))FindEdgeMethod.Invoke(null, new object[] { block, 2, 0 })!;
        var edge3 = ((int x, int y))FindEdgeMethod.Invoke(null, new object[] { block, 3, 1 })!;

        Assert.AreEqual((2, 0), edge0);
        Assert.AreEqual((1, 1), edge1);
        Assert.AreEqual((0, 0), edge2);
        Assert.AreEqual((2, 0), edge3);
    }

    static void InvokeExecuteCommand(int commandIndex, int blockSize, List<int> stack,
        ref int dp, ref int cc, TextReader input, TextWriter output)
    {
        var hDiff = commandIndex / 3;
        var lDiff = commandIndex % 3;
        var args = new object[] { hDiff, lDiff, blockSize, stack, dp, cc, input, output };
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
