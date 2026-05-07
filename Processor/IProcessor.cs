using System.IO.Pipelines;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Esolang.Processor;
#pragma warning restore IDE0130

// TODO: 将来的に Esolang.Processor.Abstractions パッケージへ切り出し予定

/// <summary>
/// Processor の共通基底。実行対象のプログラムを保持する。
/// </summary>
/// <typeparam name="TProgram">パース済みプログラムの型。</typeparam>
public interface IProcessor<TProgram>
{
    /// <summary>パース済みプログラム。</summary>
    TProgram Program { get; }
}

/// <summary>
/// <see cref="TextReader"/> / <see cref="TextWriter"/> ベースの実行 IF。
/// </summary>
/// <typeparam name="TProgram">パース済みプログラムの型。</typeparam>
public interface ITextProcessor<TProgram> : IProcessor<TProgram>
{
    /// <summary>プログラムを最後まで実行し、終了コードを返す。</summary>
    int RunToEnd(
        TextReader? input = null,
        TextWriter? output = null,
        CancellationToken cancellationToken = default);

    /// <summary>プログラムを最後まで非同期実行し、終了コードを返す。</summary>
    ValueTask<int> RunToEndAsync(
        TextReader? input = null,
        TextWriter? output = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="PipeReader"/> / <see cref="PipeWriter"/> ベースの実行 IF。
/// </summary>
/// <typeparam name="TProgram">パース済みプログラムの型。</typeparam>
public interface IPipeProcessor<TProgram> : IProcessor<TProgram>
{
    /// <summary>プログラムを最後まで実行し、終了コードを返す。</summary>
    int RunToEnd(
        PipeReader input,
        PipeWriter output,
        CancellationToken cancellationToken = default);

    /// <summary>プログラムを最後まで非同期実行し、終了コードを返す。</summary>
    ValueTask<int> RunToEndAsync(
        PipeReader input,
        PipeWriter output,
        CancellationToken cancellationToken = default);
}
