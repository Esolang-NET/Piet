using Esolang.Piet.Parser;
using Esolang.Processor;
using static Esolang.Processor.IOEvent;

namespace Esolang.Piet.Processor;

public sealed partial class PietProcessor : IEventProcessor
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<IOEvent> RunAsyncEnumerable([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const byte black = (byte)PietColor.Black;
        const byte white = (byte)PietColor.White;

        var width = program.Width;
        var height = program.Height;

        var dp = 0;
        var cc = 0;
        var cx = 0;
        var cy = 0;
        var stack = new List<int>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var blockColor = (byte)program[cx, cy];
            var blockCells = FloodFill(program.Codels, width, height, cx, cy);
            var moved = false;

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var (x, y) = FindEdge(blockCells, dp, cc);
                var nx = x + DpDx(dp);
                var ny = y + DpDy(dp);

                if (nx < 0 || nx >= width || ny < 0 || ny >= height
                    || (byte)program[nx, ny] == black)
                {
                    ApplyRetry(attempt, ref dp, ref cc);
                    continue;
                }

                var nextColor = (byte)program[nx, ny];
                if (nextColor == white)
                {
                    var wx = nx;
                    var wy = ny;
                    if (SlideWhite(program.Codels, width, height, ref wx, ref wy, dp))
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

                    var commandIndex = hDiff * 3 + lDiff;

                    // Matching old mapping:
                    // 14: Input Int, 15: Input Char, 16: Output Int, 17: Output Char
                    if (commandIndex == 14) // Input Int
                    {
                        int? value = null;
                        var ev = InputInt(v => value = v);
                        yield return ev;
                        if (value.HasValue)
                        {
                            stack.Add(value.Value);
                        }
                    }
                    else if (commandIndex == 15) // Input Char
                    {
                        char? value = null;
                        var ev = InputChar(v => value = v);
                        yield return ev;
                        if (value.HasValue)
                        {
                            stack.Add(value.Value);
                        }
                    }
                    else if (commandIndex == 16) // Output Int
                    {
                        if (stack.Count >= 1)
                        {
                            var val = Pop(stack);
                            yield return OutputInt(val);
                        }
                    }
                    else if (commandIndex == 17) // Output Char
                    {
                        if (stack.Count >= 1)
                        {
                            var val = Pop(stack);
                            yield return OutputChar((char)val);
                        }
                    }
                    else
                    {
                        ExecuteCommand(hDiff, lDiff, blockCells.Count, stack, ref dp, ref cc);
                    }
                }

                cx = nx;
                cy = ny;
                moved = true;
                break;
            }

            if (!moved)
                break;
        }

        yield return End(0);
    }
}
