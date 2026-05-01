
using Esolang.Piet;
using System.IO.Pipelines;
using System.Text;
#if NET481 
using System.Threading.Tasks;
#endif

// ascii-piet (.txt) 画像の出力例
var asciiPiet = PietSample.RunAsciiPiet();
Console.WriteLine($"RunAsciiPiet: {asciiPiet}");

// PPM (.ppm) 画像の出力例
var ppm = PietSample.RunPpm();
Console.WriteLine($"RunPpm: {ppm}");

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

PietSample.RunDotGif();
PietSample.RunDotCodel11Gif();

Console.WriteLine($"{nameof(PietSample.RunHw111Gif)}:{PietSample.RunHw111Gif()}");

partial class PietSample
{
    /// <summary>
    /// Executes the ASCII-Piet sample program and returns its text output.
    /// </summary>
    [GeneratePietMethod("ascii-piet-sample.txt")]
    public static partial string RunAsciiPiet();

    /// <summary>
    /// Executes the PPM sample program and returns its text output.
    /// </summary>
    [GeneratePietMethod("ppm-sample.ppm")]
    public static partial string RunPpm();

    /// <summary>
    /// Runs the hello-world sample and writes its output to <see cref="Console.Out"/>.
    /// </summary>
    public static void RunToConsole()
    {
        RunWithTextWriter(Console.Out);
    }

    /// <summary>
    /// Executes the no-op sample program.
    /// </summary>
    [GeneratePietMethod("no-op.png")]
    public static partial void RunNoOp();

    /// <summary>
    /// Executes the hello-world sample and returns its text output.
    /// </summary>
    [GeneratePietMethod("hello-world.png")]
    public static partial string RunToString();

    /// <summary>
    /// Executes the hello-world sample asynchronously and returns its text output.
    /// </summary>
    [GeneratePietMethod("hello-world.png")]
    public static partial Task<string> RunToTaskString();

    /// <summary>
    /// Executes the hello-world sample asynchronously and returns its text output as a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    [GeneratePietMethod("hello-world.png")]
    public static partial ValueTask<string> RunToValueTaskString();

    /// <summary>
    /// Executes the hello-world sample and returns raw output bytes.
    /// </summary>
    [GeneratePietMethod("hello-world.png")]
    public static partial IEnumerable<byte> RunToEnumerableBytes();

    /// <summary>
    /// Executes the hello-world sample and streams raw output bytes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel asynchronous enumeration.</param>
    [GeneratePietMethod("hello-world.png")]
    public static partial IAsyncEnumerable<byte> RunToAsyncEnumerableBytes(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the input/output sample using a string input source.
    /// </summary>
    /// <param name="input">Input text provided to the Piet program.</param>
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithStringInput(string input);

    /// <summary>
    /// Executes the input/output sample using a <see cref="TextReader"/> input source.
    /// </summary>
    /// <param name="input">Reader used to supply input text.</param>
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithTextReader(TextReader input);

    /// <summary>
    /// Executes the input/output sample using a <see cref="PipeReader"/> input source.
    /// </summary>
    /// <param name="input">Pipe reader used to supply UTF-8 input bytes.</param>
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithPipeReader(PipeReader input);

    /// <summary>
    /// Executes the hello-world sample and writes output bytes to a <see cref="PipeWriter"/>.
    /// </summary>
    /// <param name="output">Pipe writer that receives UTF-8 output bytes.</param>
    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithPipeWriter(PipeWriter output);

    /// <summary>
    /// Executes the hello-world sample and writes output text to a <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="output">Writer that receives program output text.</param>
    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithTextWriter(TextWriter output);

    /// <summary>
    /// Executes the single-codel GIF sample.
    /// </summary>
    [GeneratePietMethod("dot.gif", codelSize: 1)]
    public static partial void RunDotGif();

    /// <summary>
    /// Executes the single-codel GIF sample with codel size 11.
    /// </summary>
    [GeneratePietMethod("dot-codel-11.gif", codelSize: 11)]
    public static partial void RunDotCodel11Gif();

    /// <summary>
    /// Executes the codel-size-11 hello-world sample.
    /// </summary>
    /// <param name="input">Optional input text for the sample program.</param>
    [GeneratePietMethod("hw1-11.gif", codelSize: 11)]
    public static partial string RunHw111Gif(string input = "");
}
