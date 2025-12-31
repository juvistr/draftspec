using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock spec script executor for testing.
/// </summary>
class MockSpecScriptExecutor : ISpecScriptExecutor
{
    private SpecContext? _result;
    private Exception? _exception;

    public void SetResult(SpecContext? context)
    {
        _result = context;
        _exception = null;
    }

    public void SetException(Exception exception)
    {
        _exception = exception;
        _result = null;
    }

    public Task<SpecContext?> ExecuteAsync(string specFile, string outputDirectory, CancellationToken ct = default)
    {
        if (_exception != null)
            throw _exception;

        return Task.FromResult(_result);
    }
}
