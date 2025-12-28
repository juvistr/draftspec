using Microsoft.Testing.Platform.Builder;

namespace DraftSpec.TestingPlatform;

/// <summary>
/// Hook class discovered by Microsoft.Testing.Platform.MSBuild source generator.
/// This enables auto-registration of DraftSpec when using the generated entry point.
/// </summary>
public static class TestingPlatformBuilderHook
{
    /// <summary>
    /// Called by the MSBuild-generated SelfRegisteredExtensions to register DraftSpec.
    /// </summary>
    public static void AddExtensions(ITestApplicationBuilder builder, string[] args)
    {
        builder.AddDraftSpec();
    }
}
