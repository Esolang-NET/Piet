using Esolang.Interpreter;
using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using System.Text;

namespace Esolang.Piet.Interpreter;

static class PietCommandActions
{
    public static async Task<int> RunAsync(string? path, string? asciiPietText, int codelSize, bool asciiPiet, bool pietPlusPlus, CancellationToken cancellationToken)
    {
        PietProgram program;
        if (!string.IsNullOrWhiteSpace(asciiPietText))
        {
            var bytes = Encoding.ASCII.GetBytes(asciiPietText);
            program = PietParser.Parse(bytes, pietPlusPlus ? ".appp" : ".txt", codelSize, cancellationToken);
        }
        else
        {
            program = PietParser.Parse(path!, codelSize, cancellationToken);
        }

        if (asciiPiet)
            return pietPlusPlus ? WriteAsciiPietPlusPlus(program) : await WriteAsciiPietAsync(program);

        var processor = new PietProcessor(program);
        return await processor.RunToConsoleAsync(cancellationToken);
    }

    public static Task<int> ParseAsync(string path, int codelSize, bool pietPlusPlus, CancellationToken cancellationToken = default)
    {
        var program = PietParser.Parse(path, codelSize, cancellationToken);
        return pietPlusPlus ? Task.FromResult(WriteAsciiPietPlusPlus(program)) : WriteAsciiPietAsync(program);
    }

    public static Task<int> ColorsAsync(bool pietPlusPlus)
    {
        if (pietPlusPlus)
            WriteAsciiPietPlusPlusTable();
        else
            WriteAsciiPietTable();
        return Task.FromResult(0);
    }

    static Task<int> WriteAsciiPietAsync(PietProgram program)
    {
        Console.Out.Write(AsciiPietFormatter.Format(program));
        return Task.FromResult(0);
    }

    static int WriteAsciiPietPlusPlus(PietProgram program)
    {
        Console.Out.Write(AsciiPietPlusPlusFormatter.Format(program));
        return 0;
    }

    static void WriteAsciiPietTable()
    {
        (string Name, char Char, char Eol)[] entries =
        [
            ("Black",         ' ', '@'),
            ("Dark Blue",     'a', 'A'),
            ("Dark Green",    'b', 'B'),
            ("Dark Cyan",     'c', 'C'),
            ("Dark Red",      'd', 'D'),
            ("Dark Magenta",  'e', 'E'),
            ("Dark Yellow",   'f', 'F'),
            ("Blue",          'i', 'I'),
            ("Green",         'j', 'J'),
            ("Cyan",          'k', 'K'),
            ("Red",           'l', 'L'),
            ("Magenta",       'm', 'M'),
            ("Yellow",        'n', 'N'),
            ("Light Blue",    'q', 'Q'),
            ("Light Green",   'r', 'R'),
            ("Light Cyan",    's', 'S'),
            ("Light Red",     't', 'T'),
            ("Light Magenta", 'u', 'U'),
            ("Light Yellow",  'v', 'V'),
            ("White",         '?', '_'),
        ];

        Console.WriteLine("ascii-piet color encoding:");
        Console.WriteLine($"{"Color",-16}  {"Char",5}  {"EOL",5}");
        foreach (var (name, ch, eol) in entries)
            Console.WriteLine($"{name,-16}  {$"'{ch}'",5}  {$"'{eol}'",5}");
    }

    static void WriteAsciiPietPlusPlusTable()
    {
        Console.WriteLine("ascii-piet++ color encoding:");
        Console.WriteLine($"{"Index",5}  {"Char",5}");
        Console.WriteLine($"{0,5}  {$"' '",5}  (Black)");
        for (var i = 0; i < 10; i++)
            Console.WriteLine($"{1 + i,5}  {$"'{(char)('0' + i)}'",5}");
        for (var i = 0; i < 26; i++)
            Console.WriteLine($"{11 + i,5}  {$"'{(char)('a' + i)}'",5}");
        for (var i = 0; i < 26; i++)
            Console.WriteLine($"{37 + i,5}  {$"'{(char)('A' + i)}'",5}");
        Console.WriteLine($"{63,5}  {$"'~'",5}  (White)");
        Console.WriteLine($"{"EOL",-5}  {"'|' / '@'",5}  (row separators; CR/LF are ignored)");
    }
}
