using System.Text;

namespace DraftSpec.Mcp.Services;

/// <summary>
/// Provides async-local console output capture for thread-safe concurrent execution.
/// Each async execution context gets its own isolated output capture.
/// </summary>
public sealed class AsyncLocalConsoleCapture : IDisposable
{
    private static readonly AsyncLocal<TextWriter?> CurrentCapture = new();
    private static readonly RoutingTextWriter Router;
    private static readonly TextWriter OriginalOut;
    private static bool _installed;
    private static readonly object InstallLock = new();

    static AsyncLocalConsoleCapture()
    {
        OriginalOut = Console.Out;
        Router = new RoutingTextWriter(OriginalOut, () => CurrentCapture.Value);
    }

    private readonly StringWriter _capture;
    private readonly TextWriter? _previous;
    private bool _disposed;

    /// <summary>
    /// Creates a new capture scope. All Console.Write* calls in this async context
    /// will be captured until Dispose is called.
    /// </summary>
    public AsyncLocalConsoleCapture()
    {
        EnsureInstalled();
        _previous = CurrentCapture.Value;
        _capture = new StringWriter();
        CurrentCapture.Value = _capture;
    }

    /// <summary>
    /// Gets the captured output for this scope.
    /// </summary>
    public string GetCapturedOutput() => _capture.ToString();

    /// <summary>
    /// Ends the capture scope, restoring the previous capture context (if any).
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CurrentCapture.Value = _previous;
    }

    /// <summary>
    /// Ensures the routing writer is installed. Called lazily on first capture.
    /// Once installed, stays installed for the lifetime of the process.
    /// </summary>
    private static void EnsureInstalled()
    {
        if (_installed) return;
        lock (InstallLock)
        {
            if (_installed) return;
            Console.SetOut(Router);
            _installed = true;
        }
    }

    /// <summary>
    /// Resets the installation state. Only for testing purposes.
    /// </summary>
    internal static void ResetForTesting()
    {
        lock (InstallLock)
        {
            if (_installed)
            {
                Console.SetOut(OriginalOut);
                _installed = false;
            }
            CurrentCapture.Value = null;
        }
    }

    /// <summary>
    /// A TextWriter that routes writes to the async-local capture if set,
    /// otherwise to the original console output.
    /// </summary>
    private sealed class RoutingTextWriter : TextWriter
    {
        private readonly TextWriter _defaultWriter;
        private readonly Func<TextWriter?> _getCaptureWriter;

        public RoutingTextWriter(TextWriter defaultWriter, Func<TextWriter?> getCaptureWriter)
        {
            _defaultWriter = defaultWriter;
            _getCaptureWriter = getCaptureWriter;
        }

        public override Encoding Encoding => _defaultWriter.Encoding;

        private TextWriter Target => _getCaptureWriter() ?? _defaultWriter;

        public override void Write(char value) => Target.Write(value);

        public override void Write(char[] buffer, int index, int count) =>
            Target.Write(buffer, index, count);

        public override void Write(string? value) => Target.Write(value);

        public override void WriteLine() => Target.WriteLine();

        public override void WriteLine(string? value) => Target.WriteLine(value);

        public override void WriteLine(char value) => Target.WriteLine(value);

        public override void WriteLine(char[] buffer, int index, int count)
        {
            Target.Write(buffer, index, count);
            Target.WriteLine();
        }

        public override Task WriteAsync(char value) => Target.WriteAsync(value);

        public override Task WriteAsync(string? value) => Target.WriteAsync(value);

        public override Task WriteAsync(char[] buffer, int index, int count) =>
            Target.WriteAsync(buffer, index, count);

        public override Task WriteLineAsync() => Target.WriteLineAsync();

        public override Task WriteLineAsync(char value) => Target.WriteLineAsync(value);

        public override Task WriteLineAsync(string? value) => Target.WriteLineAsync(value);

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            return Task.Run(async () =>
            {
                await Target.WriteAsync(buffer, index, count);
                await Target.WriteLineAsync();
            });
        }

        public override void Flush() => Target.Flush();

        public override Task FlushAsync() => Target.FlushAsync();
    }
}
