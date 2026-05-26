using Esolang.Piet.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Piet.Interpreter.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public async Task RunAsync_Default_ReturnsZero()
    {
        var exitCode = await Program.RunAsync(Array.Empty<string>());
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void EntryPoint_Invoke_ReturnsZero()
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint!;
        var task = (Task<int>)entryPoint.Invoke(null, new object[] { Array.Empty<string>() })!;
        var exitCode = task.Result;
        Assert.AreEqual(0, exitCode);
    }
}
