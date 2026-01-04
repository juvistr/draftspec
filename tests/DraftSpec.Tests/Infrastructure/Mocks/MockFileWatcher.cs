using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock file watcher for deterministic unit testing of watch mode.
/// Uses TriggerChangeAndWaitAsync to ensure tests wait until the change is processed.
/// </summary>
public class MockFileWatcher : IFileWatcher
{
    private readonly Channel<(FileChangeInfo Change, TaskCompletionSource? Signal)> _channel =
        Channel.CreateUnbounded<(FileChangeInfo, TaskCompletionSource?)>();

    private bool _disposed;

    /// <summary>
    /// Asynchronously enumerates file changes as they are triggered.
    /// The signal is completed AFTER the consumer processes the change
    /// (when they call MoveNextAsync to get the next item).
    /// </summary>
    public async IAsyncEnumerable<FileChangeInfo> WatchAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (change, signal) in _channel.Reader.ReadAllAsync(ct))
        {
            yield return change;
            // Signal AFTER yield returns (i.e., after consumer processes the change)
            signal?.TrySetResult();
        }
    }

    /// <summary>
    /// Triggers a file change event. The change will be delivered to the consumer
    /// on their next iteration. Use this for simple scenarios where timing doesn't matter.
    /// </summary>
    public void TriggerChange(FileChangeInfo change)
    {
        if (_disposed) return;
        _channel.Writer.TryWrite((change, null));
    }

    /// <summary>
    /// Triggers a file change and waits until the consumer has processed it.
    /// This provides deterministic testing without timing-based delays.
    ///
    /// The method completes when the consumer's async foreach advances to the next item,
    /// which happens after they've finished processing the yielded change.
    /// </summary>
    /// <param name="change">The file change to trigger</param>
    /// <param name="timeout">Maximum time to wait for processing (default: 5 seconds)</param>
    public async Task TriggerChangeAndWaitAsync(FileChangeInfo change, TimeSpan? timeout = null)
    {
        if (_disposed) return;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite((change, tcs));
        await tcs.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Completes the channel, signaling no more changes will come.
    /// </summary>
    public void Complete()
    {
        _channel.Writer.Complete();
    }

    public void Dispose()
    {
        _disposed = true;
        _channel.Writer.TryComplete();
    }
}
