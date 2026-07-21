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
    public static T AddPietCommands<T>(this T rootCommand)
        where T : Command
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
        var pietPlusPlusOption = new Option<bool>("--piet-plus-plus")
        {
            Description = "Use ascii-piet++ format: interpret --ascii-piet-text as ascii-piet++, and write ascii-piet++ output when --ascii-piet is specified.",
        };

        var parseCommand = BuildParseCommand(codelSizeOption);
        var colorsCommand = BuildColorsCommand();
        rootCommand.Description = "Run Piet programs from image files.";
        rootCommand.Add(inputArgument);
        rootCommand.Add(codelSizeOption);
        rootCommand.Add(asciiPietOption);
        rootCommand.Add(asciiPietTextOption);
        rootCommand.Add(pietPlusPlusOption);
        rootCommand.Add(parseCommand);
        rootCommand.Add(colorsCommand);

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
            var pietPlusPlus = parseResult.GetValue(pietPlusPlusOption);
            return PietCommandActions.RunAsync(path, asciiPietText, codelSize, asciiPiet, pietPlusPlus, cancellationToken);
        });
        return rootCommand;
    }

    static Command BuildParseCommand(Option<int> codelSizeOption)
    {
        var inputArgument = new Argument<string>("path")
        {
            Description = "Path to a Piet image file.",
        };
        var pietPlusPlusOption = new Option<bool>("--piet-plus-plus")
        {
            Description = "Write the parsed program as ascii-piet++ text instead of ascii-piet.",
        };

        var parseCommand = new Command("parse", "Parse a Piet source and write ascii-piet (or ascii-piet++) text without a trailing newline.")
        {
            inputArgument,
            codelSizeOption,
            pietPlusPlusOption,
        };

        parseCommand.SetAction((parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(inputArgument);
            var codelSize = parseResult.GetValue(codelSizeOption);
            var pietPlusPlus = parseResult.GetValue(pietPlusPlusOption);
            return PietCommandActions.ParseAsync(path!, codelSize, pietPlusPlus, cancellationToken);
        });

        return parseCommand;
    }

    static Command BuildColorsCommand()
    {
        var pietPlusPlusOption = new Option<bool>("--piet-plus-plus")
        {
            Description = "Show the ascii-piet++ character encoding table instead of ascii-piet.",
        };

        var colorsCommand = new Command("colors", "Show the ascii-piet (or ascii-piet++) character encoding table.")
        {
            pietPlusPlusOption,
        };

        colorsCommand.SetAction(parseResult =>
        {
            var pietPlusPlus = parseResult.GetValue(pietPlusPlusOption);
            return PietCommandActions.ColorsAsync(pietPlusPlus);
        });

        return colorsCommand;
    }
}
