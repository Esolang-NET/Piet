using Esolang.Piet.Parser;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Esolang.Piet.Processor;

/// <summary>
/// Executes parsed Piet programs.
/// </summary>
public sealed class PietProcessor
{
    static readonly int[] HueTable =
    {
        -1, -1,
        0, 0, 0,
        1, 1, 1,
        2, 2, 2,
        3, 3, 3,
        4, 4, 4,
        5, 5, 5,
    };

    static readonly int[] LightnessTable =
    {
        -1, -1,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
        0, 1, 2,
    };

    /// <summary>
    /// Initializes the processor with a parsed Piet program.
    /// </summary>
    public PietProcessor(PietProgram program, TextWriter? output = null, TextReader? input = null)
    {
        Program = program;
        Output = output;
        Input = input;
    }

    /// <summary>
    /// The parsed Piet program.
    /// </summary>
    public PietProgram Program { get; }

    /// <summary>
    /// Optional default input source used by <see cref="Run()"/>.
    /// </summary>
    public TextReader? Input { get; }

    /// <summary>
    /// Optional default output destination used by <see cref="Run()"/>.
    /// </summary>
    public TextWriter? Output { get; }

    /// <summary>
    /// Executes the program.
    /// </summary>
    public void Run()
    {
        Run(Input, Output);
    }

    /// <summary>
    /// Executes the program with explicit I/O.
    /// </summary>
    public void Run(TextReader? input, TextWriter? output)
    {
        const byte black = (byte)PietColor.Black;
        const byte white = (byte)PietColor.White;

        var width = Program.Width;
        var height = Program.Height;
        var codels = Program.Codels;

        var reader = input ?? TextReader.Null;
        var writer = output ?? TextWriter.Null;

        var dp = 0;
        var cc = 0;
        var cx = 0;
        var cy = 0;
        var stack = new List<int>();

        while (true)
        {
            var blockColor = (byte)codels[(cy * width) + cx];
            var blockCells = FloodFill(codels, width, height, cx, cy);
            var moved = false;

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var edge = FindEdge(blockCells, dp, cc);
                var nx = edge.x + DpDx(dp);
                var ny = edge.y + DpDy(dp);

                if (nx < 0 || nx >= width || ny < 0 || ny >= height
                    || (byte)codels[(ny * width) + nx] == black)
                {
                    ApplyRetry(attempt, ref dp, ref cc);
                    continue;
                }

                var nextColor = (byte)codels[(ny * width) + nx];
                if (nextColor == white)
                {
                    var wx = nx;
                    var wy = ny;
                    if (SlideWhite(codels, width, height, ref wx, ref wy, dp))
                    {
                        cx = wx;
                        cy = wy;
                        moved = true;
                    }
                    else
                    {
                        ApplyRetry(attempt, ref dp, ref cc);
                    }

                    break;
                }

                if (blockColor >= 2 && nextColor >= 2)
                {
                    var hDiff = ((HueTable[nextColor] - HueTable[blockColor]) % 6 + 6) % 6;
                    var lDiff = ((LightnessTable[nextColor] - LightnessTable[blockColor]) % 3 + 3) % 3;
                    ExecuteCommand(hDiff, lDiff, blockCells.Count, stack, ref dp, ref cc, reader, writer);
                }

                cx = nx;
                cy = ny;
                moved = true;
                break;
            }

            if (!moved)
                return;
        }
    }

    /// <summary>
    /// Executes the program and collects UTF-8 output as a string.
    /// </summary>
    public string? RunAndOutputString(TextReader? input = null)
    {
        using var writer = new StringWriter();
        Run(input ?? Input, writer);
        var result = writer.ToString().TrimEnd('\0');
        return result.Length == 0 ? null : result;
    }

    static void ExecuteCommand(int hDiff, int lDiff, int blockSize,
        List<int> stack, ref int dp, ref int cc, TextReader input, TextWriter output)
    {
        switch ((hDiff * 3) + lDiff)
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
                        stack.Add(((b % a) + a) % a);
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
                    stack.Add(stack[stack.Count - 1]);
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
                {
                    var s = input.ReadLine();
                    if (int.TryParse(s, out var n))
                        stack.Add(n);
                }
                break;
            case 15:
                {
                    var ch = input.Read();
                    if (ch >= 0)
                        stack.Add(ch);
                }
                break;
            case 16:
                if (stack.Count >= 1)
                    output.Write(Pop(stack));
                break;
            case 17:
                if (stack.Count >= 1)
                    output.Write((char)Pop(stack));
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
            var idx = (y * width) + x;
            if (!visited.Add(idx))
                return false;

            if ((byte)codels[idx] != (byte)PietColor.White)
                return true;

            var nx = x + DpDx(dp);
            var ny = y + DpDy(dp);
            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                || (byte)codels[(ny * width) + nx] == (byte)PietColor.Black)
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
        rolls = ((rolls % depth) + depth) % depth;
        for (var i = 0; i < rolls; i++)
        {
            var top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            stack.Insert(stack.Count - depth + 1, top);
        }
    }

    static List<(int x, int y)> FloodFill(IReadOnlyList<PietColor> codels,
        int width, int height, int x, int y)
    {
        var color = codels[(y * width) + x];
        var visited = new bool[width * height];
        var result = new List<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();

        visited[(y * width) + x] = true;
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

        var idx = (ny * width) + nx;
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
            bool better;

            switch (dp)
            {
                case 0:
                    better = bx > bestX || (bx == bestX && (cc == 0 ? by < bestY : by > bestY));
                    break;
                case 1:
                    better = by > bestY || (by == bestY && (cc == 0 ? bx > bestX : bx < bestX));
                    break;
                case 2:
                    better = bx < bestX || (bx == bestX && (cc == 0 ? by > bestY : by < bestY));
                    break;
                default:
                    better = by < bestY || (by == bestY && (cc == 0 ? bx < bestX : bx > bestX));
                    break;
            }

            if (better)
            {
                bestX = bx;
                bestY = by;
            }
        }

        return (bestX, bestY);
    }

    static int DpDx(int dp) => dp == 0 ? 1 : (dp == 2 ? -1 : 0);

    static int DpDy(int dp) => dp == 1 ? 1 : (dp == 3 ? -1 : 0);
}
