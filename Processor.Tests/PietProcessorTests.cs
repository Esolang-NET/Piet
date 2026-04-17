using Esolang.Piet.Parser;
using System.IO;

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
    public void Run_ExecutesProgramAndWritesNumberOutput()
    {
        var program = new PietProgram(
            3,
            1,
            new[]
            {
                PietColor.LightRed,
                PietColor.Red,
                PietColor.DarkMagenta,
            });
        using var output = new StringWriter();
        var processor = new PietProcessor(program, output);

        processor.Run();

        Assert.AreEqual("1", output.ToString());
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
