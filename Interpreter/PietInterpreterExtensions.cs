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
        var inputArgument = new Argument<string?>("path")
        {
            Description = "Path to a Piet image file.",
            Arity = ArgumentArity.ZeroOrOne,
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
        var asciiPietTextOption = new Option<string?>("--ascii-piet-text")
        {
            Description = "Inline ascii-piet text to execute directly (path can be omitted).",
        };

        var parseCommand = BuildParseCommand(codelSizeOption);
        var rootCommand = new RootCommand("Run Piet programs from image files.")
        {
            inputArgument,
            codelSizeOption,
            asciiPietOption,
            asciiPietTextOption,
            parseCommand,
        };
        rootCommand.Validators.Add(parseResult =>
        {
            var path = parseResult.GetValue(inputArgument);
            var asciiPietText = parseResult.GetValue(asciiPietTextOption);
            var hasPath = !string.IsNullOrWhiteSpace(path);
            var hasAsciiPietText = !string.IsNullOrWhiteSpace(asciiPietText);

            if (!hasPath && !hasAsciiPietText)
            {
                parseResult.AddError("Specify either path or --ascii-piet-text.");
                return;
            }

            if (hasPath && hasAsciiPietText)
                parseResult.AddError("path and --ascii-piet-text cannot be specified together.");
        });
        rootCommand.SetAction((parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(inputArgument);
            var codelSize = parseResult.GetValue(codelSizeOption);
            var asciiPiet = parseResult.GetValue(asciiPietOption);
            var asciiPietText = parseResult.GetValue(asciiPietTextOption);
            return PietCommandActions.RunAsync(path, asciiPietText, codelSize, asciiPiet, cancellationToken);
        });
        return rootCommand;
    }

    static Command BuildParseCommand(Option<int> codelSizeOption)
    {
        var inputArgument = new Argument<string>("path")
        {
            Description = "Path to a Piet image file.",
        };

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
