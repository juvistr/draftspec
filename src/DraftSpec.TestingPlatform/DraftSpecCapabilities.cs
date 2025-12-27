using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Declares the capabilities supported by the DraftSpec test framework.
/// </summary>
internal sealed class DraftSpecCapabilities : ITestFrameworkCapabilities
{
    /// <summary>
    /// The capabilities supported by this framework.
    /// Currently returns an empty collection as we support the basic test execution flow.
    /// </summary>
    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities { get; } = [];
}
