using Esolang.Piet.Parser;
using Esolang.Processor;

namespace Esolang.Piet.Processor;

/// <summary>
/// Executes parsed Piet programs.
/// </summary>
/// <remarks>
/// Initializes the processor with a parsed Piet program.
/// </remarks>
public sealed partial class PietProcessor(PietProgram program, TextWriter? output = null, TextReader? input = null)
    : IProcessor<PietProgram>
{
    static readonly int[] HueTable =
    [
        -1, -1,
        0, 0, 0,
        1, 1, 1,
        2, 2, 2,
        3, 3, 3,
        4, 4, 4,
        5, 5, 5,
    ];

    static readonly int[] LightnessTable =
    [
        -1, -1,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
    ];

    /// <summary>
    /// The parsed Piet program.
    /// </summary>
    public PietProgram Program { get; } = program;

    /// <summary>
    /// Optional default input source.
    /// </summary>
    public TextReader? Input { get; } = input;

    /// <summary>
    /// Optional default output destination.
    /// </summary>
    public TextWriter? Output { get; } = output;

    /// <summary>
    /// Executes the program.
    /// </summary>
    public void Run() => RunToEnd(Input, Output);

    /// <summary>
    /// Executes the program with explicit I/O.
    /// </summary>
    public void Run(TextReader? input, TextWriter? output) => RunToEnd(input ?? Input, output ?? Output);

    /// <summary>
    /// Executes the program and collects UTF-8 output as a string.
    /// </summary>
    public string? RunAndOutputString(TextReader? input = null)
    {
        using var writer = new StringWriter();
        RunToEnd(input ?? Input, writer);
        var result = writer.ToString().TrimEnd('\0', '\r', '\n');
        return result.Length == 0 ? null : result;
    }

    /// <inheritdoc/>
    public int RunToEnd(TextReader? input = null, TextWriter? output = null, CancellationToken cancellationToken = default)
    {
        var result = RunToEndAsync(input ?? Input, output ?? Output, cancellationToken);
        if (result.IsCompleted)
            return result.GetAwaiter().GetResult();
        return result.AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public ValueTask<int> RunToEndAsync(TextReader? input = null, TextWriter? output = null, CancellationToken cancellationToken = default)
        => TextProcessorExtensions.RunToEndAsync(this, input ?? Input, output ?? Output, cancellationToken);

    static void ExecuteCommand(int hDiff, int lDiff, int blockSize,
        List<int> stack, ref int dp, ref int cc)
    {
        switch (hDiff * 3 + lDiff)
        {
            case 0:
                break;
            case 1:
                stack.Add(blockSize);
                break;
            case 2:
                if (stack.Count >= 1)
                    stack.RemoveAt(stack.Count - 1);
                break;
            case 3:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    stack.Add(b + a);
                }
                break;
            case 4:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    stack.Add(b - a);
                }
                break;
            case 5:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    stack.Add(b * a);
                }
                break;
            case 6:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    if (a != 0)
                        stack.Add(b / a);
                    else
                    {
                        stack.Add(b);
                        stack.Add(a);
                    }
                }
                break;
            case 7:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    if (a != 0)
                        stack.Add((b % a + a) % a);
                    else
                    {
                        stack.Add(b);
                        stack.Add(a);
                    }
                }
                break;
            case 8:
                if (stack.Count >= 1)
                {
                    var a = Pop(stack);
                    stack.Add(a == 0 ? 1 : 0);
                }
                break;
            case 9:
                if (stack.Count >= 2)
                {
                    var a = Pop(stack);
                    var b = Pop(stack);
                    stack.Add(b > a ? 1 : 0);
                }
                break;
            case 10:
                if (stack.Count >= 1)
                {
                    var a = Pop(stack);
                    dp = ((dp + a) % 4 + 4) % 4;
                }
                break;
            case 11:
                if (stack.Count >= 1)
                {
                    var a = Pop(stack);
                    if (Math.Abs(a) % 2 == 1)
                        cc ^= 1;
                }
                break;
            case 12:
                if (stack.Count >= 1)
                    stack.Add(stack[^1]);
                break;
            case 13:
                if (stack.Count >= 2)
                {
                    var rolls = Pop(stack);
                    var depth = Pop(stack);
                    if (depth > 0 && depth <= stack.Count)
                        Roll(stack, depth, rolls);
                }
                break;
            case 14:
            case 15:
            case 16:
            case 17:
                // Handled in IEventProcessor
                break;
        }
    }

    static void ApplyRetry(int attempt, ref int dp, ref int cc)
    {
        if (attempt % 2 == 0)
            cc ^= 1;
        else
            dp = (dp + 1) & 3;
    }

    static bool SlideWhite(IReadOnlyList<PietColor> codels, int width, int height,
        ref int x, ref int y, int dp)
    {
        var visited = new HashSet<int>();

        while (true)
        {
            var idx = y * width + x;
            if (!visited.Add(idx))
                return false;

            if ((byte)codels[idx] != (byte)PietColor.White)
                return true;

            var nx = x + DpDx(dp);
            var ny = y + DpDy(dp);
            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                || (byte)codels[ny * width + nx] == (byte)PietColor.Black)
                return false;

            x = nx;
            y = ny;
        }
    }

    static int Pop(List<int> stack)
    {
        var index = stack.Count - 1;
        var value = stack[index];
        stack.RemoveAt(index);
        return value;
    }

    static void Roll(List<int> stack, int depth, int rolls)
    {
        rolls = (rolls % depth + depth) % depth;
        for (var i = 0; i < rolls; i++)
        {
            var top = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            stack.Insert(stack.Count - depth + 1, top);
        }
    }

    static List<(int x, int y)> FloodFill(IReadOnlyList<PietColor> codels,
        int width, int height, int x, int y)
    {
        var color = codels[y * width + x];
        var visited = new bool[width * height];
        var result = new List<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();

        visited[y * width + x] = true;
        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            result.Add(cell);

            var cx = cell.x;
            var cy = cell.y;
            var nx = cx + 1;
            var ny = cy;
            EnqueueIfMatch(codels, width, height, color, nx, ny, visited, queue);

            nx = cx - 1;
            ny = cy;
            EnqueueIfMatch(codels, width, height, color, nx, ny, visited, queue);

            nx = cx;
            ny = cy + 1;
            EnqueueIfMatch(codels, width, height, color, nx, ny, visited, queue);

            nx = cx;
            ny = cy - 1;
            EnqueueIfMatch(codels, width, height, color, nx, ny, visited, queue);
        }

        return result;
    }

    static void EnqueueIfMatch(IReadOnlyList<PietColor> codels, int width, int height,
        PietColor color, int nx, int ny, bool[] visited, Queue<(int x, int y)> queue)
    {
        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            return;

        var idx = ny * width + nx;
        if (visited[idx] || codels[idx] != color)
            return;

        visited[idx] = true;
        queue.Enqueue((nx, ny));
    }

    static (int x, int y) FindEdge(List<(int x, int y)> block, int dp, int cc)
    {
        var bestX = block[0].x;
        var bestY = block[0].y;

        for (var i = 1; i < block.Count; i++)
        {
            var bx = block[i].x;
            var by = block[i].y;
            var better = dp switch
            {
                0 => bx > bestX || bx == bestX && (cc == 0 ? by < bestY : by > bestY),
                1 => by > bestY || by == bestY && (cc == 0 ? bx > bestX : bx < bestX),
                2 => bx < bestX || bx == bestX && (cc == 0 ? by > bestY : by < bestY),
                3 => by < bestY || by == bestY && (cc == 0 ? bx < bestX : bx > bestX),
                _ => throw new InvalidOperationException("Unexpected DP value"),
            };

            if (better)
            {
                bestX = bx;
                bestY = by;
            }
        }

        return (bestX, bestY);
    }

    static int DpDx(int dp) => dp switch { 0 => 1, 2 => -1, _ => 0 };

    static int DpDy(int dp) => dp switch { 1 => 1, 3 => -1, _ => 0 };
}
