using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using System.CommandLine;

var inputArgument = new Argument<string>("path")
{
    Description = "Path to a Piet image file.",
};
var rootCommand = new RootCommand("Run Piet programs from image files.")
{
    inputArgument,
};
rootCommand.SetAction(parseResult =>
{
    var path = parseResult.GetValue(inputArgument);
    var program = PietParser.Parse(path!);
    var processor = new PietProcessor(program, Console.Out, Console.In);
    processor.Run();
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();
