using System.CommandLine;

namespace Esolang.Piet.Interpreter;

/// <summary>
/// Extension methods for building the Piet interpreter command-line interface.
/// </summary>
public static class PietInterpreterExtensions
{
    /// <summary>
    /// Builds the root command for the dotnet-piet CLI.
    /// </summary>
    public static RootCommand BuildRootCommand()
    {
        var inputArgument = new Argument<string>("path")
        {
            Description = "Path to a Piet image file.",
        };
        var codelSizeOption = new Option<int>("--codel-size", "-cs")
        {
            DefaultValueFactory = _ => 1,
            Description = "Codel size to use when parsing the image.",
        };
        var asciiPietOption = new Option<bool>("--ascii-piet")
        {
            Description = "Write the parsed program as ascii-piet text without a trailing newline and exit.",
        };

        var parseCommand = BuildParseCommand(inputArgument, codelSizeOption);
        var rootCommand = new RootCommand("Run Piet programs from image files.")
        {
            inputArgument,
            codelSizeOption,
            asciiPietOption,
            parseCommand,
        };
        rootCommand.SetAction((parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(inputArgument);
            var codelSize = parseResult.GetValue(codelSizeOption);
            var asciiPiet = parseResult.GetValue(asciiPietOption);
            return PietCommandActions.RunAsync(path!, codelSize, asciiPiet, cancellationToken);
        });
        return rootCommand;
    }

    private static Command BuildParseCommand(Argument<string> inputArgument, Option<int> codelSizeOption)
    {
        var parseCommand = new Command("parse", "Parse a Piet source and write ascii-piet text without a trailing newline.")
        {
            inputArgument,
            codelSizeOption,
        };

        parseCommand.SetAction(parseResult =>
        {
            var path = parseResult.GetValue(inputArgument);
            var codelSize = parseResult.GetValue(codelSizeOption);
            return PietCommandActions.ParseAsync(path!, codelSize);
        });

        return parseCommand;
    }
}
