using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using System.Text;

namespace Esolang.Piet.Interpreter;

internal static class PietCommandActions
{
    public static Task<int> RunAsync(string? path, string? asciiPietText, int codelSize, bool asciiPiet, CancellationToken cancellationToken)
    {
        PietProgram program;
        if (!string.IsNullOrWhiteSpace(asciiPietText))
        {
            var bytes = Encoding.ASCII.GetBytes(asciiPietText);
            program = PietParser.Parse(bytes, ".txt", codelSize);
        }
        else
        {
            program = PietParser.Parse(path!, codelSize);
        }

        if (asciiPiet)
            return WriteAsciiPietAsync(program);

        var output = Console.Out;
        var input = Console.In;
        var processor = new PietProcessor(program, output, input);
        return Task.FromResult(processor.RunToEnd(cancellationToken: cancellationToken));
    }

    public static Task<int> ParseAsync(string path, int codelSize)
    {
        var program = PietParser.Parse(path, codelSize);
        return WriteAsciiPietAsync(program);
    }

    private static Task<int> WriteAsciiPietAsync(PietProgram program)
    {
        Console.Out.Write(AsciiPietFormatter.Format(program));
        return Task.FromResult(0);
    }
}
