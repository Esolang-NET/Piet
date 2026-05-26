using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Esolang.Piet.Interpreter.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void EntryPoint_Invoke_ReturnsZero()
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint!;
        var task = (Task<int>)entryPoint.Invoke(null, new object[] { Array.Empty<string>() })!;
        var exitCode = task.Result;
        Assert.AreEqual(0, exitCode);
    }
}
