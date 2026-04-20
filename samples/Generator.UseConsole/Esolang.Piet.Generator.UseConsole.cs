
using Esolang.Piet;
using System.IO.Pipelines;
using System.Text;

// ascii-piet (.txt) 画像の出力例
var asciiPiet = PietSample.RunAsciiPiet();
Console.WriteLine($"RunAsciiPiet: {asciiPiet}");

// PPM (.ppm) 画像の出力例
var ppm = PietSample.RunPpm();
Console.WriteLine($"RunPpm: {ppm}");

// GIF (.gif) 画像の出力例
var gif = PietSample.RunGif();
Console.WriteLine($"RunGif: {gif}");

Console.WriteLine("Running Piet generated sample...");

// void return — runs to Console
PietSample.RunToConsole();

// string return
var asString = PietSample.RunToString();
Console.WriteLine($"RunToString: {asString}");

// Task<string> return
var taskString = await PietSample.RunToTaskString();
Console.WriteLine($"RunToTaskString: {taskString}");

// ValueTask<string> return
var valueTaskString = await PietSample.RunToValueTaskString();
Console.WriteLine($"RunToValueTaskString: {valueTaskString}");

// IEnumerable<byte> return
var bytes = new List<byte>(PietSample.RunToEnumerableBytes());
Console.WriteLine($"RunToEnumerableBytes: {Encoding.UTF8.GetString(bytes.ToArray())}");

// IAsyncEnumerable<byte> return
var asyncBytes = new List<byte>();
await foreach (var b in PietSample.RunToAsyncEnumerableBytes(CancellationToken.None))
    asyncBytes.Add(b);
Console.WriteLine($"RunToAsyncEnumerableBytes: {Encoding.UTF8.GetString(asyncBytes.ToArray())}");

// string input parameter
var withStringInput = PietSample.RunWithStringInput("123");
Console.WriteLine($"RunWithStringInput: {withStringInput}");

// TextReader input parameter
var withTextReader = PietSample.RunWithTextReader(new StringReader("456\n"));
Console.WriteLine($"RunWithTextReader: {withTextReader}");

// PipeReader input parameter
var inputPipe = new Pipe();
await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("789\n"));
inputPipe.Writer.Complete();
var withPipeReader = PietSample.RunWithPipeReader(inputPipe.Reader);
Console.WriteLine($"RunWithPipeReader: {withPipeReader}");

// PipeWriter output parameter
var outputPipe = new Pipe();
PietSample.RunWithPipeWriter(outputPipe.Writer);
outputPipe.Writer.Complete();
var pipeWriterResult = new StreamReader(outputPipe.Reader.AsStream()).ReadToEnd();
Console.WriteLine($"RunWithPipeWriter: {pipeWriterResult}");

// TextWriter output parameter
var textWriterOutput = new StringWriter();
PietSample.RunWithTextWriter(textWriterOutput);
Console.WriteLine($"RunWithTextWriter: {textWriterOutput}");

partial class PietSample
{
    [GeneratePietMethod("ascii-piet-sample.txt")]
    public static partial string RunAsciiPiet();

    [GeneratePietMethod("ppm-sample.ppm")]
    public static partial string RunPpm();

    [GeneratePietMethod("sample.gif")]
    public static partial string RunGif();

    public static void RunToConsole()
    {
        RunWithTextWriter(Console.Out);
    }

    [GeneratePietMethod("no-op.png")]
    public static partial void RunNoOp();

    [GeneratePietMethod("hello-world.png")]
    public static partial string RunToString();

    [GeneratePietMethod("hello-world.png")]
    public static partial Task<string> RunToTaskString();

    [GeneratePietMethod("hello-world.png")]
    public static partial ValueTask<string> RunToValueTaskString();

    [GeneratePietMethod("hello-world.png")]
    public static partial IEnumerable<byte> RunToEnumerableBytes();

    [GeneratePietMethod("hello-world.png")]
    public static partial IAsyncEnumerable<byte> RunToAsyncEnumerableBytes(CancellationToken cancellationToken = default);

    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithStringInput(string input);

    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithTextReader(TextReader input);

    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithPipeReader(PipeReader input);

    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithPipeWriter(PipeWriter output);

    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithTextWriter(TextWriter output);
}
