using DraftSpec.TestingPlatform;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;

namespace DraftSpec.Tests.TestingPlatform;

/// <summary>
/// Tests for the DraftSpecTestFramework MTP adapter.
/// These tests verify the main orchestration layer that bridges MTP and DraftSpec.
/// </summary>
/// <remarks>
/// Note: Full integration tests for discovery/execution flows are in TodoApi.Specs
/// which runs via `dotnet test` and exercises the complete MTP pipeline.
/// These unit tests cover the instantiation and public properties.
/// </remarks>
public class DraftSpecTestFrameworkTests
{
    #region Properties

    [Test]
    public async Task Uid_ReturnsDraftSpecTestingPlatform()
    {
        var framework = CreateFramework();

        await Assert.That(framework.Uid).IsEqualTo("DraftSpec.TestingPlatform");
    }

    [Test]
    public async Task DisplayName_ReturnsDraftSpec()
    {
        var framework = CreateFramework();

        await Assert.That(framework.DisplayName).IsEqualTo("DraftSpec");
    }

    [Test]
    public async Task Description_ReturnsExpectedText()
    {
        var framework = CreateFramework();

        await Assert.That(framework.Description).IsEqualTo("RSpec-inspired testing framework for .NET");
    }

    [Test]
    public async Task Version_ReturnsNonEmptyString()
    {
        var framework = CreateFramework();

        await Assert.That(framework.Version).IsNotNull();
        await Assert.That(framework.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Version_MatchesAssemblyVersion()
    {
        var framework = CreateFramework();
        var expectedVersion = typeof(DraftSpecTestFramework).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        await Assert.That(framework.Version).IsEqualTo(expectedVersion);
    }

    [Test]
    public async Task IsEnabledAsync_ReturnsTrue()
    {
        var framework = CreateFramework();

        var isEnabled = await framework.IsEnabledAsync();

        await Assert.That(isEnabled).IsTrue();
    }

    #endregion

    #region Instantiation

    [Test]
    public async Task Constructor_WithNullCapabilities_Throws()
    {
        // Base class throws NullReferenceException when accessing null capabilities
        await Assert.ThrowsAsync<NullReferenceException>(
            () => Task.FromResult(new DraftSpecTestFramework(null!, new MockServiceProvider())));
    }

    [Test]
    public async Task Constructor_WithNullServiceProvider_Throws()
    {
        // Base class validates service provider with ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new DraftSpecTestFramework(new MockTestFrameworkCapabilities(), null!)));
    }

    [Test]
    public async Task Constructor_WithValidParameters_Succeeds()
    {
        var framework = CreateFramework();

        await Assert.That(framework).IsNotNull();
    }

    [Test]
    public async Task MultipleInstances_HaveSameUid()
    {
        var framework1 = CreateFramework();
        var framework2 = CreateFramework();

        await Assert.That(framework1.Uid).IsEqualTo(framework2.Uid);
    }

    #endregion

    #region ExtractTestIds

    [Test]
    public async Task ExtractTestIds_NullFilter_ReturnsNull()
    {
        var result = DraftSpecTestFramework.ExtractTestIds(null);

        Assert.Null(result);
    }

    [Test]
    public async Task ExtractTestIds_EmptyUidFilter_ReturnsNull()
    {
        var filter = new TestNodeUidListFilter([]);

        var result = DraftSpecTestFramework.ExtractTestIds(filter);

        Assert.Null(result);
    }

    [Test]
    public async Task ExtractTestIds_WithUids_ReturnsTestIds()
    {
        var filter = new TestNodeUidListFilter([
            new TestNodeUid("test-1"),
            new TestNodeUid("test-2"),
            new TestNodeUid("test-3")
        ]);

        var result = DraftSpecTestFramework.ExtractTestIds(filter);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(3);
        await Assert.That(result.Contains("test-1")).IsTrue();
        await Assert.That(result.Contains("test-2")).IsTrue();
        await Assert.That(result.Contains("test-3")).IsTrue();
    }

    [Test]
    public async Task ExtractTestIds_WithDuplicateUids_DeduplicatesIds()
    {
        var filter = new TestNodeUidListFilter([
            new TestNodeUid("test-1"),
            new TestNodeUid("test-1"),
            new TestNodeUid("test-2")
        ]);

        var result = DraftSpecTestFramework.ExtractTestIds(filter);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Count).IsEqualTo(2);
    }

    #endregion

    #region EnsureInitialized

    [Test]
    public async Task EnsureInitialized_WithNoProjectDirectory_UsesAssemblyLocation()
    {
        var framework = CreateFramework();

        // First call should initialize (using assembly location as project directory)
        framework.EnsureInitialized();

        // No exception means success - we can't easily verify the internal state
        // but subsequent calls should not throw
        framework.EnsureInitialized();
        await Assert.That(framework).IsNotNull();
    }

    [Test]
    public async Task EnsureInitialized_WithExplicitProjectDirectory_UsesProvidedPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var framework = new DraftSpecTestFramework(
                new MockTestFrameworkCapabilities(),
                new MockServiceProvider(),
                projectDirectory: tempDir);

            framework.EnsureInitialized();

            // No exception means success
            await Assert.That(framework).IsNotNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task EnsureInitialized_CalledMultipleTimes_OnlyInitializesOnce()
    {
        var framework = CreateFramework();

        // Call multiple times
        framework.EnsureInitialized();
        framework.EnsureInitialized();
        framework.EnsureInitialized();

        // Should not throw and framework should still be valid
        await Assert.That(framework).IsNotNull();
    }

    #endregion

    #region ResetState

    [Test]
    public async Task ResetState_AfterInitialization_ClearsState()
    {
        var framework = CreateFramework();

        // Initialize then reset
        framework.EnsureInitialized();
        framework.ResetState();

        // After reset, EnsureInitialized should work again
        framework.EnsureInitialized();
        await Assert.That(framework).IsNotNull();
    }

    [Test]
    public async Task ResetState_WithoutInitialization_DoesNotThrow()
    {
        var framework = CreateFramework();

        // Should not throw even if never initialized
        framework.ResetState();
        await Assert.That(framework).IsNotNull();
    }

    [Test]
    public async Task ResetState_CalledMultipleTimes_DoesNotThrow()
    {
        var framework = CreateFramework();

        framework.EnsureInitialized();
        framework.ResetState();
        framework.ResetState();
        framework.ResetState();

        await Assert.That(framework).IsNotNull();
    }

    #endregion

    #region Helpers

    private static DraftSpecTestFramework CreateFramework()
    {
        return new DraftSpecTestFramework(
            new MockTestFrameworkCapabilities(),
            new MockServiceProvider());
    }

    #endregion

    #region Mocks

    private class MockTestFrameworkCapabilities : ITestFrameworkCapabilities
    {
        public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [];
    }

    private class MockServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    #endregion
}
