namespace Esolang.Piet.Processor.Tests;

[TestClass]
public sealed class PietProcessorTests
{
    [TestMethod]
    public void Constructor_StoresProgram()
    {
        var program = new PietProgram(1, 1, new[] { PietColor.White });

        var processor = new PietProcessor(program);

        Assert.AreSame(program, processor.Program);
    }

    [TestMethod]
    public void Run_ThrowsUntilImplemented()
    {
        var processor = new PietProcessor(new PietProgram(1, 1, new[] { PietColor.White }));

        _ = Assert.ThrowsExactly<NotImplementedException>(() => processor.Run());
    }
}
