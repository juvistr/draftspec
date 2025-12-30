using DraftSpec.Scripting;

namespace DraftSpec.Tests.TestHelpers;

/// <summary>
/// Mock implementation of IScriptHost for testing.
/// </summary>
public class MockScriptHost : IScriptHost
{
    private readonly Dictionary<string, Func<CancellationToken, Task<SpecContext?>>> _results = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Exception> _exceptions = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ExecuteCalls { get; } = [];
    public int ResetCallCount { get; private set; }

    /// <summary>
    /// Configures the mock to return a specific context for a path.
    /// </summary>
    public MockScriptHost WithResult(string path, SpecContext? context)
    {
        _results[path] = _ => Task.FromResult(context);
        return this;
    }

    /// <summary>
    /// Configures the mock to return a context built by a factory.
    /// </summary>
    public MockScriptHost WithResult(string path, Func<SpecContext?> factory)
    {
        _results[path] = _ => Task.FromResult(factory());
        return this;
    }

    /// <summary>
    /// Configures the mock to throw an exception for a path.
    /// </summary>
    public MockScriptHost ThrowsFor(string path, Exception exception)
    {
        _exceptions[path] = exception;
        return this;
    }

    /// <summary>
    /// Configures the mock to return a successful spec context with a single spec.
    /// </summary>
    public MockScriptHost WithSuccessfulSpec(string path, string description, Action? body = null)
    {
        _results[path] = _ =>
        {
            var context = new SpecContext("Root");
            if (body != null)
            {
                context.AddSpec(new SpecDefinition(description, body));
            }
            else
            {
                // Pending spec - no body means it's pending
                context.AddSpec(new SpecDefinition(description));
            }
            return Task.FromResult<SpecContext?>(context);
        };
        return this;
    }

    /// <summary>
    /// Configures the mock to return null (no specs) for a path.
    /// </summary>
    public MockScriptHost WithNoSpecs(string path)
    {
        _results[path] = _ => Task.FromResult<SpecContext?>(null);
        return this;
    }

    public Task<SpecContext?> ExecuteAsync(string csxFilePath, CancellationToken cancellationToken = default)
    {
        ExecuteCalls.Add(csxFilePath);

        // Check for configured exception first
        if (_exceptions.TryGetValue(csxFilePath, out var exception))
        {
            throw exception;
        }

        // Check for configured result
        if (_results.TryGetValue(csxFilePath, out var resultFactory))
        {
            return resultFactory(cancellationToken);
        }

        throw new InvalidOperationException($"No mock result configured for path: {csxFilePath}");
    }

    public void Reset()
    {
        ResetCallCount++;
    }
}
