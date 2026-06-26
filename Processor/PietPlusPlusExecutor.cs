using Esolang.Piet.Parser;
using Esolang.Processor;
using static Esolang.Processor.IOEvent;

namespace Esolang.Piet.Processor;

readonly struct PietPlusPlusValue
{
    readonly int _intVal;
    readonly List<PietPlusPlusValue>? _stack;

    PietPlusPlusValue(int intVal) => _intVal = intVal;
    PietPlusPlusValue(List<PietPlusPlusValue> stack) => _stack = stack;

    internal int IntVal => _intVal;
    internal List<PietPlusPlusValue>? Stack => _stack;
    internal bool IsStack => _stack != null;

    internal static PietPlusPlusValue FromInt(int i) => new(i);
    internal static PietPlusPlusValue FromStack(List<PietPlusPlusValue> s) => new(s);
}

static class PietPlusPlusExecutor
{
    const byte Black = 0;
    const byte White = 63;

    // cmd = DG*16 + DR*4 + DB
    // DR = (R2-R1+4)%4, DG = (G2-G1+2)%2, DB = (B2-B1+4)%4
    // where R=(c>>4)&3, G=(c>>2)&3, B=c&3
    static int ComputeCmd(byte c1, byte c2)
    {
        var dr = (((c2 >> 4) & 3) - ((c1 >> 4) & 3) + 4) % 4;
        var dg = (((c2 >> 2) & 3) - ((c1 >> 2) & 3) + 2) % 2;
        var db = ((c2 & 3) - (c1 & 3) + 4) % 4;
        return dg * 16 + dr * 4 + db;
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
            if ((byte)codels[idx] != White)
                return true;
            var nx = x + PietProcessor.DpDx(dp);
            var ny = y + PietProcessor.DpDy(dp);
            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                || (byte)codels[ny * width + nx] == Black)
                return false;
            x = nx;
            y = ny;
        }
    }

    static PietPlusPlusValue Pop(List<PietPlusPlusValue> stack)
    {
        var index = stack.Count - 1;
        var value = stack[index];
        stack.RemoveAt(index);
        return value;
    }

    static void Roll(List<PietPlusPlusValue> stack, int depth, int rolls)
    {
        rolls = (rolls % depth + depth) % depth;
        for (var i = 0; i < rolls; i++)
        {
            var top = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            stack.Insert(stack.Count - depth + 1, top);
        }
    }

    internal static async IAsyncEnumerable<IOEvent> RunAsyncEnumerable(
        PietProgram program,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var width = program.Width;
        var height = program.Height;

        var dp = 0;
        var cc = 0;
        var cx = 0;
        var cy = 0;
        // parentStacks tracks the navigation path for Up/Down commands
        var parentStacks = new Stack<List<PietPlusPlusValue>>();
        var currentStack = new List<PietPlusPlusValue>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var blockColor = (byte)program[cx, cy];
            var blockCells = PietProcessor.FloodFill(program.Codels, width, height, cx, cy);
            var moved = false;

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var (x, y) = PietProcessor.FindEdge(blockCells, dp, cc);
                var nx = x + PietProcessor.DpDx(dp);
                var ny = y + PietProcessor.DpDy(dp);

                if (nx < 0 || nx >= width || ny < 0 || ny >= height
                    || (byte)program[nx, ny] == Black)
                {
                    PietProcessor.ApplyRetry(attempt, ref dp, ref cc);
                    continue;
                }

                var nextColor = (byte)program[nx, ny];
                if (nextColor == White)
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
                        PietProcessor.ApplyRetry(attempt, ref dp, ref cc);
                    }
                    break;
                }

                if (blockColor is not (Black or White) && nextColor is not (Black or White))
                {
                    var cmd = ComputeCmd(blockColor, nextColor);
                    switch (cmd)
                    {
                        case 0: // Noop
                            break;
                        case 1: // Dup
                            if (currentStack.Count >= 1)
                                currentStack.Add(currentStack[^1]);
                            break;
                        case 2: // PushDown: if second item is a stack, pop top and push into that stack
                            if (currentStack.Count >= 2 && currentStack[^2].IsStack)
                            {
                                var val = Pop(currentStack); // pops top; second (a stack) becomes top
                                currentStack[^1].Stack!.Add(val);
                            }
                            break;
                        case 3: // Add
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal + a.IntVal));
                            }
                            break;
                        case 4: // PushInt: push block size as integer
                            currentStack.Add(PietPlusPlusValue.FromInt(blockCells.Count));
                            break;
                        case 5: // Roll
                            if (currentStack.Count >= 2)
                            {
                                var rolls = Pop(currentStack).IntVal;
                                var depth = Pop(currentStack).IntVal;
                                if (depth > 0 && depth <= currentStack.Count)
                                    Roll(currentStack, depth, rolls);
                            }
                            break;
                        case 6: // PullUp: if top is a stack, pop from it into currentStack
                            if (currentStack.Count >= 1 && currentStack[^1].IsStack
                                && currentStack[^1].Stack!.Count > 0)
                            {
                                var nested = currentStack[^1].Stack!;
                                var val = nested[^1];
                                nested.RemoveAt(nested.Count - 1);
                                currentStack.Add(val);
                            }
                            break;
                        case 7: // Subtract
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal - a.IntVal));
                            }
                            break;
                        case 8: // PushStack: push a new empty stack onto currentStack
                            currentStack.Add(PietPlusPlusValue.FromStack([]));
                            break;
                        case 9: // RollContext: no-op (complex hierarchy roll)
                            break;
                        case 10: // Up: navigate to parent stack
                            if (parentStacks.Count > 0)
                                currentStack = parentStacks.Pop();
                            break;
                        case 11: // Multiply
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal * a.IntVal));
                            }
                            break;
                        case 12: // Pop
                            if (currentStack.Count >= 1)
                                Pop(currentStack);
                            break;
                        case 13: // PushUp: pop top and push to parent stack
                            if (parentStacks.Count > 0 && currentStack.Count >= 1)
                            {
                                var val = Pop(currentStack);
                                parentStacks.Peek().Add(val);
                            }
                            break;
                        case 14: // Down: navigate into top stack (if top is a stack)
                            if (currentStack.Count >= 1 && currentStack[^1].IsStack)
                            {
                                parentStacks.Push(currentStack);
                                currentStack = currentStack[^1].Stack!;
                            }
                            break;
                        case 15: // Divide
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                if (a.IntVal != 0)
                                    currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal / a.IntVal));
                                else
                                {
                                    currentStack.Add(b);
                                    currentStack.Add(a);
                                }
                            }
                            break;
                        case 16: // Mod
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                if (a.IntVal != 0)
                                    currentStack.Add(PietPlusPlusValue.FromInt((b.IntVal % a.IntVal + a.IntVal) % a.IntVal));
                                else
                                {
                                    currentStack.Add(b);
                                    currentStack.Add(a);
                                }
                            }
                            break;
                        case 17: // Equal
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal == a.IntVal ? 1 : 0));
                            }
                            break;
                        case 18: // InChar
                            {
                                char? value = null;
                                var ev = InputChar(v => value = v);
                                yield return ev;
                                if (value.HasValue)
                                    currentStack.Add(PietPlusPlusValue.FromInt(value.Value));
                                break;
                            }
                        case 19: // Read (bitmap read - no-op)
                            break;
                        case 20: // Negate
                            if (currentStack.Count >= 1)
                            {
                                var a = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(-a.IntVal));
                            }
                            break;
                        case 21: // Lesser
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal < a.IntVal ? 1 : 0));
                            }
                            break;
                        case 22: // OutInt
                            if (currentStack.Count >= 1)
                            {
                                var val = Pop(currentStack);
                                yield return OutputInt(val.IntVal);
                            }
                            break;
                        case 23: // Write (bitmap write - no-op)
                            break;
                        case 24: // Not
                            if (currentStack.Count >= 1)
                            {
                                var a = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(a.IntVal == 0 ? 1 : 0));
                            }
                            break;
                        case 25: // Size: push size of top nested stack (if top is a stack)
                            if (currentStack.Count >= 1 && currentStack[^1].IsStack)
                                currentStack.Add(PietPlusPlusValue.FromInt(currentStack[^1].Stack!.Count));
                            break;
                        case 26: // OutChar
                            if (currentStack.Count >= 1)
                            {
                                var val = Pop(currentStack);
                                yield return OutputChar((char)val.IntVal);
                            }
                            break;
                        case 27: // Pointer: rotate DP
                            if (currentStack.Count >= 1)
                            {
                                var a = Pop(currentStack);
                                dp = ((dp + a.IntVal) % 4 + 4) % 4;
                            }
                            break;
                        case 28: // Greater
                            if (currentStack.Count >= 2)
                            {
                                var a = Pop(currentStack);
                                var b = Pop(currentStack);
                                currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal > a.IntVal ? 1 : 0));
                            }
                            break;
                        case 29: // InInt
                            {
                                int? value = null;
                                var ev = InputInt(v => value = v);
                                yield return ev;
                                if (value.HasValue)
                                    currentStack.Add(PietPlusPlusValue.FromInt(value.Value));
                                break;
                            }
                        case 30: // Depth: push nesting depth
                            currentStack.Add(PietPlusPlusValue.FromInt(parentStacks.Count));
                            break;
                        case 31: // Toggle: toggle CC
                            if (currentStack.Count >= 1)
                            {
                                var a = Pop(currentStack);
                                if (Math.Abs(a.IntVal) % 2 == 1)
                                    cc ^= 1;
                            }
                            break;
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
