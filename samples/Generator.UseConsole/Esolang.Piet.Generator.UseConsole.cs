using Esolang.Piet;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;

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

partial class PietSample
{
    [GeneratePietMethod("no-op.png")]
    public static partial void RunToConsole();

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
    public static partial string RunWithPipeReader(PipeReader input);

    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithPipeWriter(PipeWriter output);
}
