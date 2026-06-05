using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace Esolang.Piet.Generator;

partial class MethodGenerator
{
    /// <summary>
    /// The generated file name for the shared Piet interpreter runtime.
    /// </summary>
    public const string GeneratePietRuntimeFileName = GeneratedMethodsFileName;

    /// <summary>
    /// The features that may be needed by the generated methods.
    /// </summary>
    [Flags]
    enum GeneratorFeatures
    {
        /// <summary>No features needed.</summary>
        None = 0b000000000,
        /// <summary>Synchronous runtime needed.</summary>
        Sync = 0b000000001,
        /// <summary>Asynchronous runtime needed.</summary>
        Async = 0b000000010,
        /// <summary>Enumerable runtime needed.</summary>
        Enumerable = 0b000000100,
        /// <summary>Asynchronous enumerable runtime needed.</summary>
        AsyncEnumerable = 0b000001000,
        /// <summary>Logging support needed.</summary>
        UseLogging = 0b000010000,
        /// <summary>Synchronous Piet++ runtime needed.</summary>
        SyncPlusPlus = 0b000100000,
        /// <summary>Asynchronous Piet++ runtime needed.</summary>
        AsyncPlusPlus = 0b001000000,
        /// <summary>Enumerable Piet++ runtime needed.</summary>
        EnumerablePlusPlus = 0b010000000,
        /// <summary>Asynchronous enumerable Piet++ runtime needed.</summary>
        AsyncEnumerablePlusPlus = 0b100000000,
    }

    /// <summary>
    /// Tries to generate the source code for the shared Piet interpreter runtime if needed.
    /// </summary>
    static void AppendPietRuntimeSource(GeneratorFeatures features, LanguageVersion languageVersion, StringBuilder builder)
    {
        if (features == GeneratorFeatures.None)
            return;
        var enableLogging = (features & GeneratorFeatures.UseLogging) != 0;
        // `logger? = null,` or string.Emtpy
        var withLoggingParameter = enableLogging ? "global::Microsoft.Extensions.Logging.ILogger? logger = null," : string.Empty;
        // `logger: logger,` or string.Empty
        var useLoggingParameter = enableLogging ? "logger: logger," : string.Empty;

        // LogExecuting(logger, a1, a2, a3)
        Func<string, string, string, string> callLogExecuting
             = enableLogging
                ? static (a1, a2, a3) => $$"""
                global::Esolang.Piet.__Generated.LoggerUtilities.LogExecuting(logger, hDiff * 3 + lDiff, blockSize, index);

                """
                : static (_, _, _) => string.Empty;
        var fileOrInternal = IsLanguageVersionAtLeastCSharp11(languageVersion) ? "file" : "internal";
        builder.AppendLine($$"""

        namespace Esolang.Piet.__Generated
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            {{fileOrInternal}} static class PietRuntime
            {
                private static readonly int[] s_hue = new int[]
                {
                    -1, -1,
                     0,  0,  0,
                     1,  1,  1,
                     2,  2,  2,
                     3,  3,  3,
                     4,  4,  4,
                     5,  5,  5,
                };

                private static readonly int[] s_light = new int[]
                {
                    -1, -1,
                     0,  1,  2,
                     0,  1,  2,
                     0,  1,  2,
                     0,  1,  2,
                     0,  1,  2,
                     0,  1,  2,
                };

                private static int Pop(List<int> stack)
                {
                    int val = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    return val;
                }

                private static void Roll(List<int> stack, int depth, int rolls)
                {
                    rolls = ((rolls % depth) + depth) % depth;
                    for (int i = 0; i < rolls; i++)
                    {
                        int top = stack[stack.Count - 1];
                        stack.RemoveAt(stack.Count - 1);
                        stack.Insert(stack.Count - depth + 1, top);
                    }
                }

                private static List<Tuple<int, int>> FloodFill(
                    byte[] codels, int width, int height, int x, int y)
                {
                    byte color = codels[y * width + x];
                    var visited = new bool[width * height];
                    var result = new List<Tuple<int, int>>();
                    var queue = new Queue<Tuple<int, int>>();

                    visited[y * width + x] = true;
                    queue.Enqueue(Tuple.Create(x, y));

                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        result.Add(cell);

                        int cx = cell.Item1, cy = cell.Item2;
                        int[] dxs = { 1, -1, 0, 0 };
                        int[] dys = { 0, 0, 1, -1 };

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + dxs[d], ny = cy + dys[d];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int idx = ny * width + nx;
                                if (!visited[idx] && codels[idx] == color)
                                {
                                    visited[idx] = true;
                                    queue.Enqueue(Tuple.Create(nx, ny));
                                }
                            }
                        }
                    }

                    return result;
                }

                private static void ApplyRetry(int attempt, ref int dp, ref int cc)
                {
                    if (attempt % 2 == 0)
                        cc ^= 1;
                    else
                        dp = (dp + 1) & 3;
                }

                private static Tuple<int, int> FindEdge(
                    List<Tuple<int, int>> block,
                    int dp, int cc)
                {
                    int bestX = block[0].Item1, bestY = block[0].Item2;
                    for (int i = 1; i < block.Count; i++)
                    {
                        int bx = block[i].Item1, by = block[i].Item2;
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
                            case 3:
                                better = by < bestY || (by == bestY && (cc == 0 ? bx < bestX : bx > bestX));
                                break;
                            default:
                                throw new InvalidOperationException("Unexpected DP value");
                        }
                        if (better) { bestX = bx; bestY = by; }
                    }
                    return Tuple.Create(bestX, bestY);
                }

                private static int DpDx(int dp) => dp == 0 ? 1 : (dp == 2 ? -1 : 0);
                private static int DpDy(int dp) => dp == 1 ? 1 : (dp == 3 ? -1 : 0);

                private static bool SlideWhite(
                    byte[] codels, int width, int height,
                    ref int x, ref int y, int dp)
                {
                    var visited = new HashSet<int>();
                    while (true)
                    {
                        int idx = y * width + x;
                        if (!visited.Add(idx)) return false;
                        if (codels[idx] != 1) return true;
                        int nx = x + DpDx(dp);
                        int ny = y + DpDy(dp);
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height
                            || codels[ny * width + nx] == 0)
                            return false;
                        x = nx;
                        y = ny;
                    }
                }
        """);
        if ((features & (GeneratorFeatures.Sync | GeneratorFeatures.Enumerable)) > 0)
            builder.AppendLine($$"""
                private static void ExecuteCmd(
                    int hDiff, int lDiff, int blockSize,
                    List<int> stack, ref int dp, ref int cc,
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    Action<byte> writeOutput,
                    {{withLoggingParameter}}
                    int index = 0)
                {
                    {{callLogExecuting("hDiff * 3 + lDiff", "blockSize", "index")}}
                    switch (hDiff * 3 + lDiff)
                    {
                        case 0: break;
                        case 1: stack.Add(blockSize); break;
                        case 2:
                            if (stack.Count >= 1) stack.RemoveAt(stack.Count - 1);
                            break;
                        case 3:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b + a); }
                            break;
                        case 4:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b - a); }
                            break;
                        case 5:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b * a); }
                            break;
                        case 6:
                            if (stack.Count >= 2)
                            {
                                int a = Pop(stack), b = Pop(stack);
                                if (a != 0) stack.Add(b / a);
                                else { stack.Add(b); stack.Add(a); }
                            }
                            break;
                        case 7:
                            if (stack.Count >= 2)
                            {
                                int a = Pop(stack), b = Pop(stack);
                                if (a != 0) stack.Add(((b % a) + a) % a);
                                else { stack.Add(b); stack.Add(a); }
                            }
                            break;
                        case 8:
                            if (stack.Count >= 1) { int a = Pop(stack); stack.Add(a == 0 ? 1 : 0); }
                            break;
                        case 9:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b > a ? 1 : 0); }
                            break;
                        case 10:
                            if (stack.Count >= 1) { int a = Pop(stack); dp = ((dp + a) % 4 + 4) % 4; }
                            break;
                        case 11:
                            if (stack.Count >= 1)
                            {
                                int a = Pop(stack);
                                if (Math.Abs(a) % 2 == 1) cc ^= 1;
                            }
                            break;
                        case 12:
                            if (stack.Count >= 1) stack.Add(stack[stack.Count - 1]);
                            break;
                        case 13:
                            if (stack.Count >= 2)
                            {
                                int rolls = Pop(stack), depth = Pop(stack);
                                if (depth > 0 && depth <= stack.Count)
                                    Roll(stack, depth, rolls);
                            }
                            break;
                        case 14:
                            {
                                var n = readNumber();
                                if (n.HasValue) stack.Add(n.Value);
                            }
                            break;
                        case 15:
                            {
                                var ch = readChar();
                                if (ch.HasValue) stack.Add(ch.Value);
                            }
                            break;
                        case 16:
                            if (stack.Count >= 1)
                                writeOutput((byte)Pop(stack));
                            break;
                        case 17:
                            if (stack.Count >= 1)
                                writeOutput((byte)Pop(stack));
                            break;
                    }
                }
                
                public static void Execute(
                    byte[] codels, 
                    int width,
                    int height,
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    Action<byte> writeOutput,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                    => ExecuteCore(
                        codels,
                        width,
                        height,
                        readNumber,
                        readChar,
                        writeOutput,
                        ref DpInit,
                        ref CcInit,
                        index: 0,
                        {{useLoggingParameter}}
                        cancellationToken: cancellationToken);

                public static void Execute(
                    byte[] codels,
                    int width,
                    int height,
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    Action<byte> writeOutput,
                    {{withLoggingParameter}}
                    int index = 0,
                    CancellationToken cancellationToken = default)
                {
                    int dp = 0;
                    int cc = 0;
                    ExecuteCore(
                        codels,
                        width,
                        height,
                        readNumber,
                        readChar,
                        writeOutput,
                        ref dp, 
                        ref cc,
                        index: index, 
                        {{useLoggingParameter}}
                        cancellationToken: cancellationToken);
                }
                private static int DpInit = 0;
                private static int CcInit = 0;
        """);

        if ((features & (GeneratorFeatures.Async | GeneratorFeatures.AsyncEnumerable)) > 0)
            builder.AppendLine($$"""
                private static async global::System.Threading.Tasks.ValueTask<(int dp, int cc)> ExecuteCmdAsync(
                    int hDiff, int lDiff, int blockSize,
                    List<int> stack, int dp, int cc,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    Action<byte> writeOutput,
                    int index = 0,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                {

                    {{callLogExecuting("hDiff * 3 + lDiff", "blockSize", "index")}}
                    switch (hDiff * 3 + lDiff)
                    {
                        case 0: break;
                        case 1: stack.Add(blockSize); break;
                        case 2:
                            if (stack.Count >= 1) stack.RemoveAt(stack.Count - 1);
                            break;
                        case 3:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b + a); }
                            break;
                        case 4:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b - a); }
                            break;
                        case 5:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b * a); }
                            break;
                        case 6:
                            if (stack.Count >= 2)
                            {
                                int a = Pop(stack), b = Pop(stack);
                                if (a != 0) stack.Add(b / a);
                                else { stack.Add(b); stack.Add(a); }
                            }
                            break;
                        case 7:
                            if (stack.Count >= 2)
                            {
                                int a = Pop(stack), b = Pop(stack);
                                if (a != 0) stack.Add(((b % a) + a) % a);
                                else { stack.Add(b); stack.Add(a); }
                            }
                            break;
                        case 8:
                            if (stack.Count >= 1) { int a = Pop(stack); stack.Add(a == 0 ? 1 : 0); }
                            break;
                        case 9:
                            if (stack.Count >= 2) { int a = Pop(stack), b = Pop(stack); stack.Add(b > a ? 1 : 0); }
                            break;
                        case 10:
                            if (stack.Count >= 1)
                            {
                                int a = Pop(stack);
                                dp = ((dp + a) % 4 + 4) % 4;
                            }
                            break;
                        case 11:
                            if (stack.Count >= 1)
                            {
                                int a = Pop(stack);
                                if (Math.Abs(a) % 2 == 1)
                                    cc ^= 1;
                            }
                            break;
                        case 12:
                            if (stack.Count >= 1) stack.Add(stack[stack.Count - 1]);
                            break;
                        case 13:
                            if (stack.Count >= 2)
                            {
                                int rolls = Pop(stack), depth = Pop(stack);
                                if (depth > 0 && depth <= stack.Count)
                                    Roll(stack, depth, rolls);
                            }
                            break;
                        case 14:
                            {
                                var n = await readNumberAsync(cancellationToken).ConfigureAwait(false);
                                if (n.HasValue) stack.Add(n.Value);
                            }
                            break;
                        case 15:
                            {
                                var ch = await readCharAsync(cancellationToken).ConfigureAwait(false);
                                if (ch.HasValue) stack.Add(ch.Value);
                            }
                            break;
                        case 16:
                            if (stack.Count >= 1)
                                writeOutput((byte)Pop(stack));
                            break;
                        case 17:
                            if (stack.Count >= 1)
                                writeOutput((byte)Pop(stack));
                            break;
                    }

                    return (dp, cc);
                }
                
                public static global::System.Threading.Tasks.Task ExecuteAsync(
                    byte[] codels, int width, int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    Action<byte> writeOutput,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                    => ExecuteAsync(
                        codels,
                        width,
                        height,
                        readNumberAsync,
                        readCharAsync,
                        writeOutput,
                        {{useLoggingParameter}}
                        index: 0, 
                        cancellationToken: cancellationToken);

                public static global::System.Threading.Tasks.Task ExecuteAsync(
                    byte[] codels, int width, int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    Action<byte> writeOutput,
                    {{withLoggingParameter}}
                    int index = 0,
                    CancellationToken cancellationToken = default)
                    => ExecuteCoreAsync(
                        codels,
                        width,
                        height,
                        readNumberAsync,
                        readCharAsync,
                        writeOutput,
                        index: index,
                        {{useLoggingParameter}}
                        cancellationToken: cancellationToken);
        """);

        if ((features & (GeneratorFeatures.Sync | GeneratorFeatures.Enumerable)) != 0)
            builder.AppendLine($$"""
                internal static void ExecuteCore(
                    byte[] codels,
                    int width,
                    int height,
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    Action<byte> writeOutput,
                    ref int dp,
                    ref int cc,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                    => ExecuteCore(
                        codels,
                        width,
                        height,
                        readNumber,
                        readChar,
                        writeOutput,
                        ref dp,
                        ref cc,
                        {{useLoggingParameter}}
                        cancellationToken: cancellationToken);

                [EditorBrowsable(EditorBrowsableState.Never)]
                internal static void ExecuteCore(
                    byte[] codels,
                    int width, 
                    int height, 
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    Action<byte> writeOutput,
                    ref int dp,
                    ref int cc,
                    int index = 0,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                {
                    const byte Black = 0;
                    const byte White = 1;

                    int cx = 0, cy = 0;
                    var stack = new List<int>();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);

                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int nx = edge.Item1 + DpDx(dp);
                            int ny = edge.Item2 + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];

                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
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
                                int hDiff = ((s_hue[nextColor] - s_hue[blockColor]) % 6 + 6) % 6;
                                int lDiff = ((s_light[nextColor] - s_light[blockColor]) % 3 + 3) % 3;

                                ExecuteCmd(
                                    hDiff, lDiff, blockCells.Count,
                                    stack, ref dp, ref cc,
                                    readNumber,
                                    readChar,
                                    writeOutput,
                                    {{useLoggingParameter}}
                                    index: index);
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
        """);

        if ((features & (GeneratorFeatures.Async | GeneratorFeatures.AsyncEnumerable)) != 0)
            builder.AppendLine($$"""
                // ============================================================
                // 2. Async: 全出力を Task<byte[]> として返す
                // ============================================================

                [EditorBrowsable(EditorBrowsableState.Never)]
                private static async global::System.Threading.Tasks.Task ExecuteCoreAsync(
                    byte[] codels, int width, int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    Action<byte> writeOutput,
                    int index = 0,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                {
                    const byte Black = 0;
                    const byte White = 1;

                    int dp = 0;
                    int cc = 0;
                    int cx = 0, cy = 0;
                    var stack = new List<int>();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);

                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int nx = edge.Item1 + DpDx(dp);
                            int ny = edge.Item2 + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];

                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
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
                                int hDiff = ((s_hue[nextColor] - s_hue[blockColor]) % 6 + 6) % 6;
                                int lDiff = ((s_light[nextColor] - s_light[blockColor]) % 3 + 3) % 3;

                                (dp, cc) = await ExecuteCmdAsync(
                                    hDiff, lDiff, blockCells.Count,
                                    stack, dp, cc,
                                    readNumberAsync, readCharAsync, writeOutput,
                                    {{useLoggingParameter}}
                                    index: index,
                                    cancellationToken: cancellationToken
                                ).ConfigureAwait(false);
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
        """);

        if ((features & GeneratorFeatures.Enumerable) != 0)
            builder.AppendLine($$"""
                // ============================================================
                // 3. Enumerable: 実行しながら逐次 byte を返す
                // ============================================================

                [EditorBrowsable(EditorBrowsableState.Never)]
                internal static IEnumerable<byte> ExecuteEnumerable(
                    byte[] codels, int width, int height,
                    Func<int?> readNumber,
                    Func<int?> readChar,
                    int index = 0,
                    {{withLoggingParameter}}
                    CancellationToken cancellationToken = default)
                {
                    const byte Black = 0;
                    const byte White = 1;

                    int dp = 0;
                    int cc = 0;
                    int cx = 0, cy = 0;
                    var stack = new List<int>();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);

                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int nx = edge.Item1 + DpDx(dp);
                            int ny = edge.Item2 + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];

                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
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
                                int hDiff = ((s_hue[nextColor] - s_hue[blockColor]) % 6 + 6) % 6;
                                int lDiff = ((s_light[nextColor] - s_light[blockColor]) % 3 + 3) % 3;

                                switch (hDiff * 3 + lDiff)
                                {
                                    case 16:
                                        if (stack.Count >= 1)
                                            yield return (byte)Pop(stack);
                                        yield break;

                                    case 17:
                                        if (stack.Count >= 1)
                                            yield return (byte)Pop(stack);
                                        yield break;

                                    default:
                                        ExecuteCmd(
                                            hDiff, lDiff, blockCells.Count,
                                            stack, ref dp, ref cc,
                                            readNumber, readChar, _ => { }, 
                                            {{useLoggingParameter}} 
                                            index: index);
                                        yield break;
                                }
                            }

                            cx = nx;
                            cy = ny;
                            moved = true;
                            break;
                        }

                        if (!moved)
                            yield break;
                    }
                }
        """);

        if ((features & GeneratorFeatures.AsyncEnumerable) != 0)
            builder.AppendLine($$"""
                // ============================================================
                // 4. AsyncEnumerable: 非同期で逐次 byte を返す
                // ============================================================

                [EditorBrowsable(EditorBrowsableState.Never)]
                internal static async IAsyncEnumerable<byte> ExecuteAsyncEnumerable(
                    byte[] codels,
                    int width,
                    int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    {{withLoggingParameter}}
                    int index = 0,
                    [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
                {
                    const byte Black = 0;
                    const byte White = 1;

                    int dp = 0;
                    int cc = 0;
                    int cx = 0, cy = 0;
                    var stack = new List<int>();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);

                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int nx = edge.Item1 + DpDx(dp);
                            int ny = edge.Item2 + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];

                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
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
                                int hDiff = ((s_hue[nextColor] - s_hue[blockColor]) % 6 + 6) % 6;
                                int lDiff = ((s_light[nextColor] - s_light[blockColor]) % 3 + 3) % 3;

                                var buffer = new List<byte>();
                                (dp, cc) = await ExecuteCmdAsync(
                                    hDiff, lDiff, blockCells.Count,
                                    stack, dp, cc,
                                    readNumberAsync, readCharAsync,
                                    b => buffer.Add(b),
                                    index: index,
                                    {{useLoggingParameter}}
                                    cancellationToken: cancellationToken
                                ).ConfigureAwait(false);

                                foreach (var b in buffer)
                                    yield return b;
                            }

                            cx = nx;
                            cy = ny;
                            moved = true;
                            break;
                        }

                        if (!moved)
                            yield break;
                    }
                }

        """);

        builder.AppendLine("""
            }

        """);

        if ((features & GeneratorFeatures.UseLogging) != 0)
            builder.AppendLine($$"""
            
            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            {{fileOrInternal}} static class LoggerUtilities
            {
                private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, int, int, int, global::System.Exception?> ExecutingCommand =
                    global::Microsoft.Extensions.Logging.LoggerMessage.Define<int, int, int>(global::Microsoft.Extensions.Logging.LogLevel.Trace, 0, "Executing Piet command: op={Op}, value={Value}, index={Index}");

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                public static void LogExecuting(global::Microsoft.Extensions.Logging.ILogger? logger, int op, int value, int index)
                {
                    if (logger is not global::Microsoft.Extensions.Logging.ILogger l || !l.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Trace)) return;
                    ExecutingCommand(l, op, value, index, null);
                }
            }

        """);
        builder.AppendLine("""
        }
        """);

    }

    /// <summary>
    /// The generated file name for the shared Piet++ interpreter runtime.
    /// </summary>
    public const string GeneratePietPlusPlusRuntimeFileName = GeneratedMethodsFileName;

    /// <summary>
    /// Tries to generate the source code for the shared Piet++ interpreter runtime if needed.
    /// </summary>
    static void AppendPietPlusPlusRuntimeSource(GeneratorFeatures features, LanguageVersion languageVersion, StringBuilder builder)
    {
        var ppFeatures = features & (GeneratorFeatures.SyncPlusPlus | GeneratorFeatures.AsyncPlusPlus
            | GeneratorFeatures.EnumerablePlusPlus | GeneratorFeatures.AsyncEnumerablePlusPlus);
        if (ppFeatures == GeneratorFeatures.None)
            return;

        var needsSync = (ppFeatures & (GeneratorFeatures.SyncPlusPlus | GeneratorFeatures.EnumerablePlusPlus)) != 0;
        var needsAsync = (ppFeatures & (GeneratorFeatures.AsyncPlusPlus | GeneratorFeatures.AsyncEnumerablePlusPlus)) != 0;
        var needsEnumerable = (ppFeatures & GeneratorFeatures.EnumerablePlusPlus) != 0;
        var needsAsyncEnumerable = (ppFeatures & GeneratorFeatures.AsyncEnumerablePlusPlus) != 0;

        var fileOrInternal = IsLanguageVersionAtLeastCSharp11(languageVersion) ? "file" : "internal";
        builder.AppendLine($$"""

        namespace Esolang.Piet.__Generated
        {
            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            {{fileOrInternal}} readonly struct PietPlusPlusValue
            {
                readonly int _intVal;
                readonly List<PietPlusPlusValue>? _stack;
                PietPlusPlusValue(int i) => _intVal = i;
                PietPlusPlusValue(List<PietPlusPlusValue> s) => _stack = s;
                internal int IntVal => _intVal;
                internal List<PietPlusPlusValue>? Stack => _stack;
                internal bool IsStack => _stack != null;
                internal static PietPlusPlusValue FromInt(int i) => new PietPlusPlusValue(i);
                internal static PietPlusPlusValue FromStack(List<PietPlusPlusValue> s) => new PietPlusPlusValue(s);
            }

            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            {{fileOrInternal}} static class PietPlusPlusRuntime
            {
                private const byte Black = 0;
                private const byte White = 63;

                // cmd = DG*16 + DR*4 + DB
                private static int ComputeCmd(byte c1, byte c2)
                {
                    int dr = (((c2 >> 4) & 3) - ((c1 >> 4) & 3) + 4) % 4;
                    int dg = (((c2 >> 2) & 3) - ((c1 >> 2) & 3) + 2) % 2;
                    int db = ((c2 & 3) - (c1 & 3) + 4) % 4;
                    return dg * 16 + dr * 4 + db;
                }

                private static PietPlusPlusValue Pop(List<PietPlusPlusValue> stack)
                {
                    var v = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    return v;
                }

                private static void Roll(List<PietPlusPlusValue> stack, int depth, int rolls)
                {
                    rolls = ((rolls % depth) + depth) % depth;
                    for (int i = 0; i < rolls; i++)
                    {
                        var top = stack[stack.Count - 1];
                        stack.RemoveAt(stack.Count - 1);
                        stack.Insert(stack.Count - depth + 1, top);
                    }
                }

                private static List<Tuple<int, int>> FloodFill(
                    byte[] codels, int width, int height, int x, int y)
                {
                    byte color = codels[y * width + x];
                    var visited = new bool[width * height];
                    var result = new List<Tuple<int, int>>();
                    var queue = new Queue<Tuple<int, int>>();
                    visited[y * width + x] = true;
                    queue.Enqueue(Tuple.Create(x, y));
                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        result.Add(cell);
                        int cx2 = cell.Item1, cy2 = cell.Item2;
                        int[] dxs = { 1, -1, 0, 0 };
                        int[] dys = { 0, 0, 1, -1 };
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx2 + dxs[d], ny = cy2 + dys[d];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                int idx = ny * width + nx;
                                if (!visited[idx] && codels[idx] == color)
                                {
                                    visited[idx] = true;
                                    queue.Enqueue(Tuple.Create(nx, ny));
                                }
                            }
                        }
                    }
                    return result;
                }

                private static void ApplyRetry(int attempt, ref int dp, ref int cc)
                {
                    if (attempt % 2 == 0) cc ^= 1;
                    else dp = (dp + 1) & 3;
                }

                private static Tuple<int, int> FindEdge(List<Tuple<int, int>> block, int dp, int cc)
                {
                    int bestX = block[0].Item1, bestY = block[0].Item2;
                    for (int i = 1; i < block.Count; i++)
                    {
                        int bx = block[i].Item1, by = block[i].Item2;
                        bool better;
                        switch (dp)
                        {
                            case 0: better = bx > bestX || (bx == bestX && (cc == 0 ? by < bestY : by > bestY)); break;
                            case 1: better = by > bestY || (by == bestY && (cc == 0 ? bx > bestX : bx < bestX)); break;
                            case 2: better = bx < bestX || (bx == bestX && (cc == 0 ? by > bestY : by < bestY)); break;
                            case 3: better = by < bestY || (by == bestY && (cc == 0 ? bx < bestX : bx > bestX)); break;
                            default: throw new InvalidOperationException("Unexpected DP value");
                        }
                        if (better) { bestX = bx; bestY = by; }
                    }
                    return Tuple.Create(bestX, bestY);
                }

                private static int DpDx(int dp) => dp == 0 ? 1 : (dp == 2 ? -1 : 0);
                private static int DpDy(int dp) => dp == 1 ? 1 : (dp == 3 ? -1 : 0);

                private static bool SlideWhite(
                    byte[] codels, int width, int height,
                    ref int x, ref int y, int dp)
                {
                    var visited = new HashSet<int>();
                    while (true)
                    {
                        int idx = y * width + x;
                        if (!visited.Add(idx)) return false;
                        if (codels[idx] != White) return true;
                        int nx = x + DpDx(dp);
                        int ny = y + DpDy(dp);
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height
                            || codels[ny * width + nx] == Black)
                            return false;
                        x = nx;
                        y = ny;
                    }
                }

        """);

        if (needsSync)
            builder.AppendLine("""
                private static void ExecuteCore(
                    byte[] codels, int width, int height,
                    Func<int?> readNumber, Func<int?> readChar, Action<byte> writeOutput,
                    CancellationToken cancellationToken)
                {
                    int dp = 0, cc = 0, cx = 0, cy = 0;
                    var parentStacks = new Stack<List<PietPlusPlusValue>>();
                    var currentStack = new List<PietPlusPlusValue>();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);
                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int x = edge.Item1, y = edge.Item2;
                            int nx = x + DpDx(dp), ny = y + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];
                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
                                if (SlideWhite(codels, width, height, ref wx, ref wy, dp))
                                    { cx = wx; cy = wy; moved = true; }
                                else
                                    ApplyRetry(attempt, ref dp, ref cc);
                                break;
                            }

                            if (blockColor != Black && blockColor != White && nextColor != Black && nextColor != White)
                            {
                                int cmd = ComputeCmd(blockColor, nextColor);
                                switch (cmd)
                                {
                                    case 0: break;
                                    case 1:
                                        if (currentStack.Count >= 1) currentStack.Add(currentStack[currentStack.Count - 1]);
                                        break;
                                    case 2:
                                        if (currentStack.Count >= 2 && currentStack[currentStack.Count - 2].IsStack)
                                        {
                                            var val = Pop(currentStack);
                                            currentStack[currentStack.Count - 1].Stack!.Add(val);
                                        }
                                        break;
                                    case 3:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal + a.IntVal)); }
                                        break;
                                    case 4:
                                        currentStack.Add(PietPlusPlusValue.FromInt(blockCells.Count));
                                        break;
                                    case 5:
                                        if (currentStack.Count >= 2) { int rolls = Pop(currentStack).IntVal; int depth = Pop(currentStack).IntVal; if (depth > 0 && depth <= currentStack.Count) Roll(currentStack, depth, rolls); }
                                        break;
                                    case 6:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack && currentStack[currentStack.Count - 1].Stack!.Count > 0)
                                        { var nested = currentStack[currentStack.Count - 1].Stack!; var v = nested[nested.Count - 1]; nested.RemoveAt(nested.Count - 1); currentStack.Add(v); }
                                        break;
                                    case 7:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal - a.IntVal)); }
                                        break;
                                    case 8:
                                        currentStack.Add(PietPlusPlusValue.FromStack(new List<PietPlusPlusValue>()));
                                        break;
                                    case 9: break;
                                    case 10:
                                        if (parentStacks.Count > 0) currentStack = parentStacks.Pop();
                                        break;
                                    case 11:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal * a.IntVal)); }
                                        break;
                                    case 12:
                                        if (currentStack.Count >= 1) Pop(currentStack);
                                        break;
                                    case 13:
                                        if (parentStacks.Count > 0 && currentStack.Count >= 1) { var v = Pop(currentStack); parentStacks.Peek().Add(v); }
                                        break;
                                    case 14:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                        { parentStacks.Push(currentStack); currentStack = currentStack[currentStack.Count - 1].Stack!; }
                                        break;
                                    case 15:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal / a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 16:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(((b.IntVal % a.IntVal) + a.IntVal) % a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 17:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal == a.IntVal ? 1 : 0)); }
                                        break;
                                    case 18:
                                        { var n = readChar(); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 19: break;
                                    case 20:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(-a.IntVal)); }
                                        break;
                                    case 21:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal < a.IntVal ? 1 : 0)); }
                                        break;
                                    case 22:
                                        if (currentStack.Count >= 1) { var v = Pop(currentStack); writeOutput((byte)v.IntVal); }
                                        break;
                                    case 23: break;
                                    case 24:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(a.IntVal == 0 ? 1 : 0)); }
                                        break;
                                    case 25:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                            currentStack.Add(PietPlusPlusValue.FromInt(currentStack[currentStack.Count - 1].Stack!.Count));
                                        break;
                                    case 26:
                                        if (currentStack.Count >= 1) { var v = Pop(currentStack); writeOutput((byte)v.IntVal); }
                                        break;
                                    case 27:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); dp = ((dp + a.IntVal) % 4 + 4) % 4; }
                                        break;
                                    case 28:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal > a.IntVal ? 1 : 0)); }
                                        break;
                                    case 29:
                                        { var n = readNumber(); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 30:
                                        currentStack.Add(PietPlusPlusValue.FromInt(parentStacks.Count));
                                        break;
                                    case 31:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); if (Math.Abs(a.IntVal) % 2 == 1) cc ^= 1; }
                                        break;
                                }
                            }

                            cx = nx; cy = ny; moved = true; break;
                        }
                        if (!moved) break;
                    }
                }

                public static void Execute(
                    byte[] codels, int width, int height,
                    Func<int?> readNumber, Func<int?> readChar, Action<byte> writeOutput,
                    CancellationToken cancellationToken = default)
                    => ExecuteCore(codels, width, height, readNumber, readChar, writeOutput, cancellationToken);

                public static void Execute(
                    byte[] codels, int width, int height,
                    Func<int?> readNumber, Func<int?> readChar, Action<byte> writeOutput,
                    int index = 0, CancellationToken cancellationToken = default)
                    => ExecuteCore(codels, width, height, readNumber, readChar, writeOutput, cancellationToken);

        """);

        if (needsEnumerable)
            builder.AppendLine("""
                public static IEnumerable<byte> ExecuteEnumerable(
                    byte[] codels, int width, int height,
                    Func<int?> readNumber, Func<int?> readChar,
                    CancellationToken cancellationToken = default)
                {
                    var bytes = new List<byte>();
                    ExecuteCore(codels, width, height, readNumber, readChar,
                        b => bytes.Add(b), cancellationToken);
                    return bytes;
                }

        """);

        if (needsAsync)
            builder.AppendLine("""
                public static async global::System.Threading.Tasks.ValueTask ExecuteAsync(
                    byte[] codels, int width, int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    Action<byte> writeOutput,
                    CancellationToken cancellationToken = default)
                {
                    int dp = 0, cc = 0, cx = 0, cy = 0;
                    var parentStacks = new Stack<List<PietPlusPlusValue>>();
                    var currentStack = new List<PietPlusPlusValue>();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);
                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int x = edge.Item1, y = edge.Item2;
                            int nx = x + DpDx(dp), ny = y + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];
                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
                                if (SlideWhite(codels, width, height, ref wx, ref wy, dp))
                                    { cx = wx; cy = wy; moved = true; }
                                else
                                    ApplyRetry(attempt, ref dp, ref cc);
                                break;
                            }

                            if (blockColor != Black && blockColor != White && nextColor != Black && nextColor != White)
                            {
                                int cmd = ComputeCmd(blockColor, nextColor);
                                switch (cmd)
                                {
                                    case 0: break;
                                    case 1:
                                        if (currentStack.Count >= 1) currentStack.Add(currentStack[currentStack.Count - 1]);
                                        break;
                                    case 2:
                                        if (currentStack.Count >= 2 && currentStack[currentStack.Count - 2].IsStack)
                                        { var val = Pop(currentStack); currentStack[currentStack.Count - 1].Stack!.Add(val); }
                                        break;
                                    case 3:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal + a.IntVal)); }
                                        break;
                                    case 4:
                                        currentStack.Add(PietPlusPlusValue.FromInt(blockCells.Count));
                                        break;
                                    case 5:
                                        if (currentStack.Count >= 2) { int rolls = Pop(currentStack).IntVal; int depth = Pop(currentStack).IntVal; if (depth > 0 && depth <= currentStack.Count) Roll(currentStack, depth, rolls); }
                                        break;
                                    case 6:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack && currentStack[currentStack.Count - 1].Stack!.Count > 0)
                                        { var nested = currentStack[currentStack.Count - 1].Stack!; var v = nested[nested.Count - 1]; nested.RemoveAt(nested.Count - 1); currentStack.Add(v); }
                                        break;
                                    case 7:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal - a.IntVal)); }
                                        break;
                                    case 8:
                                        currentStack.Add(PietPlusPlusValue.FromStack(new List<PietPlusPlusValue>()));
                                        break;
                                    case 9: break;
                                    case 10:
                                        if (parentStacks.Count > 0) currentStack = parentStacks.Pop();
                                        break;
                                    case 11:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal * a.IntVal)); }
                                        break;
                                    case 12:
                                        if (currentStack.Count >= 1) Pop(currentStack);
                                        break;
                                    case 13:
                                        if (parentStacks.Count > 0 && currentStack.Count >= 1) { var v = Pop(currentStack); parentStacks.Peek().Add(v); }
                                        break;
                                    case 14:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                        { parentStacks.Push(currentStack); currentStack = currentStack[currentStack.Count - 1].Stack!; }
                                        break;
                                    case 15:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal / a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 16:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(((b.IntVal % a.IntVal) + a.IntVal) % a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 17:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal == a.IntVal ? 1 : 0)); }
                                        break;
                                    case 18:
                                        { var n = await readCharAsync(cancellationToken).ConfigureAwait(false); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 19: break;
                                    case 20:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(-a.IntVal)); }
                                        break;
                                    case 21:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal < a.IntVal ? 1 : 0)); }
                                        break;
                                    case 22:
                                        if (currentStack.Count >= 1) { var v = Pop(currentStack); writeOutput((byte)v.IntVal); }
                                        break;
                                    case 23: break;
                                    case 24:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(a.IntVal == 0 ? 1 : 0)); }
                                        break;
                                    case 25:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                            currentStack.Add(PietPlusPlusValue.FromInt(currentStack[currentStack.Count - 1].Stack!.Count));
                                        break;
                                    case 26:
                                        if (currentStack.Count >= 1) { var v = Pop(currentStack); writeOutput((byte)v.IntVal); }
                                        break;
                                    case 27:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); dp = ((dp + a.IntVal) % 4 + 4) % 4; }
                                        break;
                                    case 28:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal > a.IntVal ? 1 : 0)); }
                                        break;
                                    case 29:
                                        { var n = await readNumberAsync(cancellationToken).ConfigureAwait(false); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 30:
                                        currentStack.Add(PietPlusPlusValue.FromInt(parentStacks.Count));
                                        break;
                                    case 31:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); if (Math.Abs(a.IntVal) % 2 == 1) cc ^= 1; }
                                        break;
                                }
                            }

                            cx = nx; cy = ny; moved = true; break;
                        }
                        if (!moved) break;
                    }
                }

        """);

        if (needsAsyncEnumerable)
            builder.AppendLine("""
                public static async IAsyncEnumerable<byte> ExecuteAsyncEnumerable(
                    byte[] codels, int width, int height,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readNumberAsync,
                    Func<CancellationToken, global::System.Threading.Tasks.ValueTask<int?>> readCharAsync,
                    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
                {
                    await global::System.Threading.Tasks.Task.Yield();

                    int dp = 0, cc = 0, cx = 0, cy = 0;
                    var parentStacks = new Stack<List<PietPlusPlusValue>>();
                    var currentStack = new List<PietPlusPlusValue>();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        byte blockColor = codels[cy * width + cx];
                        var blockCells = FloodFill(codels, width, height, cx, cy);
                        bool moved = false;

                        for (int attempt = 0; attempt < 8; attempt++)
                        {
                            var edge = FindEdge(blockCells, dp, cc);
                            int x = edge.Item1, y = edge.Item2;
                            int nx = x + DpDx(dp), ny = y + DpDy(dp);

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height
                                || codels[ny * width + nx] == Black)
                            {
                                ApplyRetry(attempt, ref dp, ref cc);
                                continue;
                            }

                            byte nextColor = codels[ny * width + nx];
                            if (nextColor == White)
                            {
                                int wx = nx, wy = ny;
                                if (SlideWhite(codels, width, height, ref wx, ref wy, dp))
                                    { cx = wx; cy = wy; moved = true; }
                                else
                                    ApplyRetry(attempt, ref dp, ref cc);
                                break;
                            }

                            if (blockColor != Black && blockColor != White && nextColor != Black && nextColor != White)
                            {
                                int cmd = ComputeCmd(blockColor, nextColor);
                                byte? outByte = null;
                                switch (cmd)
                                {
                                    case 0: break;
                                    case 1:
                                        if (currentStack.Count >= 1) currentStack.Add(currentStack[currentStack.Count - 1]);
                                        break;
                                    case 2:
                                        if (currentStack.Count >= 2 && currentStack[currentStack.Count - 2].IsStack)
                                        { var val = Pop(currentStack); currentStack[currentStack.Count - 1].Stack!.Add(val); }
                                        break;
                                    case 3:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal + a.IntVal)); }
                                        break;
                                    case 4:
                                        currentStack.Add(PietPlusPlusValue.FromInt(blockCells.Count));
                                        break;
                                    case 5:
                                        if (currentStack.Count >= 2) { int rolls = Pop(currentStack).IntVal; int depth = Pop(currentStack).IntVal; if (depth > 0 && depth <= currentStack.Count) Roll(currentStack, depth, rolls); }
                                        break;
                                    case 6:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack && currentStack[currentStack.Count - 1].Stack!.Count > 0)
                                        { var nested = currentStack[currentStack.Count - 1].Stack!; var v = nested[nested.Count - 1]; nested.RemoveAt(nested.Count - 1); currentStack.Add(v); }
                                        break;
                                    case 7:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal - a.IntVal)); }
                                        break;
                                    case 8:
                                        currentStack.Add(PietPlusPlusValue.FromStack(new List<PietPlusPlusValue>()));
                                        break;
                                    case 9: break;
                                    case 10:
                                        if (parentStacks.Count > 0) currentStack = parentStacks.Pop();
                                        break;
                                    case 11:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal * a.IntVal)); }
                                        break;
                                    case 12:
                                        if (currentStack.Count >= 1) Pop(currentStack);
                                        break;
                                    case 13:
                                        if (parentStacks.Count > 0 && currentStack.Count >= 1) { var v = Pop(currentStack); parentStacks.Peek().Add(v); }
                                        break;
                                    case 14:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                        { parentStacks.Push(currentStack); currentStack = currentStack[currentStack.Count - 1].Stack!; }
                                        break;
                                    case 15:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal / a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 16:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); if (a.IntVal != 0) currentStack.Add(PietPlusPlusValue.FromInt(((b.IntVal % a.IntVal) + a.IntVal) % a.IntVal)); else { currentStack.Add(b); currentStack.Add(a); } }
                                        break;
                                    case 17:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal == a.IntVal ? 1 : 0)); }
                                        break;
                                    case 18:
                                        { var n = await readCharAsync(cancellationToken).ConfigureAwait(false); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 19: break;
                                    case 20:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(-a.IntVal)); }
                                        break;
                                    case 21:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal < a.IntVal ? 1 : 0)); }
                                        break;
                                    case 22:
                                        if (currentStack.Count >= 1) outByte = (byte)Pop(currentStack).IntVal;
                                        break;
                                    case 23: break;
                                    case 24:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(a.IntVal == 0 ? 1 : 0)); }
                                        break;
                                    case 25:
                                        if (currentStack.Count >= 1 && currentStack[currentStack.Count - 1].IsStack)
                                            currentStack.Add(PietPlusPlusValue.FromInt(currentStack[currentStack.Count - 1].Stack!.Count));
                                        break;
                                    case 26:
                                        if (currentStack.Count >= 1) outByte = (byte)Pop(currentStack).IntVal;
                                        break;
                                    case 27:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); dp = ((dp + a.IntVal) % 4 + 4) % 4; }
                                        break;
                                    case 28:
                                        if (currentStack.Count >= 2) { var a = Pop(currentStack); var b = Pop(currentStack); currentStack.Add(PietPlusPlusValue.FromInt(b.IntVal > a.IntVal ? 1 : 0)); }
                                        break;
                                    case 29:
                                        { var n = await readNumberAsync(cancellationToken).ConfigureAwait(false); if (n.HasValue) currentStack.Add(PietPlusPlusValue.FromInt(n.Value)); }
                                        break;
                                    case 30:
                                        currentStack.Add(PietPlusPlusValue.FromInt(parentStacks.Count));
                                        break;
                                    case 31:
                                        if (currentStack.Count >= 1) { var a = Pop(currentStack); if (Math.Abs(a.IntVal) % 2 == 1) cc ^= 1; }
                                        break;
                                }

                                if (outByte.HasValue) yield return outByte.Value;
                            }

                            cx = nx; cy = ny; moved = true; break;
                        }
                        if (!moved) yield break;
                    }
                }

        """);

        builder.AppendLine("""
            }
        }
        """);

    }
}
