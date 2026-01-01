using DraftSpec.Cli;
using DraftSpec.Cli.Pipeline;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock implementation of IConfigApplier for testing.
/// </summary>
public class MockConfigApplier : IConfigApplier
{
    /// <summary>
    /// Gets the list of options that were passed to ApplyConfig.
    /// </summary>
    public List<CliOptions> ApplyConfigCalls { get; } = [];

    /// <summary>
    /// Gets whether ApplyConfig was called.
    /// </summary>
    public bool ApplyCalled => ApplyConfigCalls.Count > 0;

    /// <summary>
    /// Gets the last options passed to ApplyConfig, or null if not called.
    /// </summary>
    public CliOptions? LastOptions => ApplyConfigCalls.Count > 0 ? ApplyConfigCalls[^1] : null;

    public void ApplyConfig(CliOptions options)
    {
        ApplyConfigCalls.Add(options);
    }
}
