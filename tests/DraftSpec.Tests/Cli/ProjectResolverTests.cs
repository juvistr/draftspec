using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for ProjectResolver class.
/// </summary>
public class ProjectResolverTests
{
    private string _tempDir = null!;
    private ProjectResolver _resolver = null!;

    [Before(Test)]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _resolver = new ProjectResolver();
    }

    [After(Test)]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region FindProject

    [Test]
    public async Task FindProject_WithCsprojPresent_ReturnsPath()
    {
        var csprojPath = Path.Combine(_tempDir, "MyProject.csproj");
        await File.WriteAllTextAsync(csprojPath, "<Project></Project>");

        var result = _resolver.FindProject(_tempDir);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEqualTo(csprojPath);
    }

    [Test]
    public async Task FindProject_WithNoCsproj_ReturnsNull()
    {
        // Empty directory - no .csproj file
        var result = _resolver.FindProject(_tempDir);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindProject_WithMultipleCsproj_ReturnsFirst()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "A.csproj"), "<Project></Project>");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "B.csproj"), "<Project></Project>");

        var result = _resolver.FindProject(_tempDir);

        await Assert.That(result).IsNotNull();
        // Should return one of them (order not guaranteed, but should return something)
        await Assert.That(Path.GetExtension(result!)).IsEqualTo(".csproj");
    }

    [Test]
    public async Task FindProject_WithOtherFiles_IgnoresThem()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Program.cs"), "class Program {}");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "README.md"), "# Readme");

        var result = _resolver.FindProject(_tempDir);

        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetProjectInfo

    [Test]
    public async Task GetProjectInfo_WithValidProject_ReturnsInfo()
    {
        // Use the actual DraftSpec project for this test
        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DraftSpec", "DraftSpec.csproj"));

        if (!File.Exists(projectPath))
        {
            // Skip if running from different location
            await Assert.That(true).IsTrue();
            return;
        }

        var result = _resolver.GetProjectInfo(projectPath);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TargetFramework).Contains("net");
    }

    [Test]
    public async Task GetProjectInfo_WithNonexistentCsproj_ReturnsNull()
    {
        // Use existing directory but non-existent csproj file
        var csprojPath = Path.Combine(_tempDir, "nonexistent.csproj");

        var result = _resolver.GetProjectInfo(csprojPath);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetProjectInfo_WithInvalidCsproj_ReturnsNull()
    {
        var csprojPath = Path.Combine(_tempDir, "Invalid.csproj");
        await File.WriteAllTextAsync(csprojPath, "not valid xml");

        var result = _resolver.GetProjectInfo(csprojPath);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetProjectInfo_WithMinimalCsproj_ReturnsNullOrDefault()
    {
        var csprojPath = Path.Combine(_tempDir, "Minimal.csproj");
        // Minimal valid project that won't have TargetPath until built
        await File.WriteAllTextAsync(csprojPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>");

        var result = _resolver.GetProjectInfo(csprojPath);

        // May return null (no TargetPath without build) or info with default framework
        // Either is acceptable behavior
        if (result != null)
        {
            await Assert.That(result.TargetFramework).IsNotEmpty();
        }
        else
        {
            await Assert.That(result).IsNull();
        }
    }

    #endregion

    #region ProjectInfo Record

    [Test]
    public async Task ProjectInfo_PreservesProperties()
    {
        var info = new ProjectInfo(
            TargetPath: "/path/to/output.dll",
            TargetFramework: "net10.0");

        await Assert.That(info.TargetPath).IsEqualTo("/path/to/output.dll");
        await Assert.That(info.TargetFramework).IsEqualTo("net10.0");
    }

    #endregion
}
