using DraftSpec.Cli.Interactive;
using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of ISpecSelector for testing.
/// </summary>
public class MockSpecSelector : ISpecSelector
{
    private SpecSelectionResult _result = SpecSelectionResult.Success([], [], 0);
    private Exception? _throwOnSelect;

    public List<IReadOnlyList<DiscoveredSpec>> SelectAsyncCalls { get; } = [];

    /// <summary>
    /// Configure the result returned by SelectAsync.
    /// </summary>
    public MockSpecSelector WithResult(SpecSelectionResult result)
    {
        _result = result;
        return this;
    }

    /// <summary>
    /// Configure the selector to return selected specs.
    /// </summary>
    public MockSpecSelector WithSelection(params string[] specDisplayNames)
    {
        _result = SpecSelectionResult.Success(
            specDisplayNames.Select(n => $"test.spec.csx:{n}").ToList(),
            specDisplayNames.ToList(),
            specDisplayNames.Length);
        return this;
    }

    /// <summary>
    /// Configure the selector to return a cancelled result.
    /// </summary>
    public MockSpecSelector Cancelled()
    {
        _result = SpecSelectionResult.Cancel();
        return this;
    }

    /// <summary>
    /// Configure the selector to throw an exception.
    /// </summary>
    public MockSpecSelector Throws(Exception exception)
    {
        _throwOnSelect = exception;
        return this;
    }

    public Task<SpecSelectionResult> SelectAsync(
        IReadOnlyList<DiscoveredSpec> specs,
        CancellationToken ct = default)
    {
        SelectAsyncCalls.Add(specs);

        if (_throwOnSelect is not null)
            throw _throwOnSelect;

        // Update total count based on actual specs provided
        if (_result.SelectedSpecIds.Count > 0)
        {
            _result = SpecSelectionResult.Success(
                _result.SelectedSpecIds,
                _result.SelectedDisplayNames,
                specs.Count);
        }

        return Task.FromResult(_result);
    }
}
