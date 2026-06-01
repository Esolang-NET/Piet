using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using Esolang.Processor;
using System.Text;

namespace Esolang.Piet.Interpreter;

static class PietCommandActions
{
    public static async Task<int> RunAsync(string? path, string? asciiPietText, int codelSize, bool asciiPiet, CancellationToken cancellationToken)
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
            return await WriteAsciiPietAsync(program);

        var output = Console.Out;
        var input = Console.In;
        var processor = new PietProcessor(program);
        return await ExecuteRunAsync(processor, input, output, cancellationToken).ConfigureAwait(false);
    }

    static async Task<int> ExecuteRunAsync(PietProcessor processor, TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        await foreach (var ioEvent in processor.RunAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            switch (ioEvent)
            {
                case InputCharEvent charInput:
                    {
                        var buffer = new char[1];
                        var count = await input.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                        if (count > 0) charInput.Write(buffer[0]);
                    }
                    break;
                case InputIntEvent intInput:
                    {
                        var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (int.TryParse(line, out var i))
                        {
                            intInput.Write(i);
                        }
                    }
                    break;
                case OutputCharEvent charOutput:
                    await output.WriteAsync(charOutput.Output).ConfigureAwait(false);
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case OutputIntEvent intOutput:
                    await output.WriteLineAsync(intOutput.Output.ToString()).ConfigureAwait(false);
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case EndEvent endEvent:
                    return endEvent.ExitCode;
            }
        }
        return 0;
    }

    public static Task<int> ParseAsync(string path, int codelSize)
    {
        var program = PietParser.Parse(path, codelSize);
        return WriteAsciiPietAsync(program);
    }

    static Task<int> WriteAsciiPietAsync(PietProgram program)
    {
        Console.Out.Write(AsciiPietFormatter.Format(program));
        return Task.FromResult(0);
    }
}
