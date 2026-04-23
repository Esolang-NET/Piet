using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using System.CommandLine;

var inputArgument = new Argument<string>("path")
{
    Description = "Path to a Piet image file.",
};
var codelSizeOption = new Option<int>("--codel-size", "-cs")
{
    DefaultValueFactory = _ => 1,
    Description = "Codel size to use when parsing the image.",
};
var rootCommand = new RootCommand("Run Piet programs from image files.")
{
    inputArgument,
    codelSizeOption,
};
rootCommand.SetAction(parseResult =>
{
    var path = parseResult.GetValue(inputArgument);
    var codelSize = parseResult.GetValue(codelSizeOption);
    var program = PietParser.Parse(path!, codelSize);
    var originalOutput = Console.Out;
    var originalInput = Console.In;
    try {
        var output = originalOutput;
        var input = originalInput;
        var processor = new PietProcessor(program, output, input);
        processor.Run();
        return 0;
    } finally
    {
        Console.SetOut(originalOutput);
        Console.SetIn(originalInput);
    }
});

return await rootCommand.Parse(args).InvokeAsync();
