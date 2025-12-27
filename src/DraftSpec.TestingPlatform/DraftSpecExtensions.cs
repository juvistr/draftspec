using Microsoft.Testing.Platform.Builder;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Extension methods for registering DraftSpec with Microsoft.Testing.Platform.
/// </summary>
public static class DraftSpecExtensions
{
    /// <summary>
    /// Adds DraftSpec test framework to the test application builder.
    /// </summary>
    /// <param name="builder">The test application builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ITestApplicationBuilder AddDraftSpec(this ITestApplicationBuilder builder)
    {
        builder.RegisterTestFramework(
            _ => new DraftSpecCapabilities(),
            (capabilities, serviceProvider) => new DraftSpecTestFramework(capabilities, serviceProvider));

        return builder;
    }
}
