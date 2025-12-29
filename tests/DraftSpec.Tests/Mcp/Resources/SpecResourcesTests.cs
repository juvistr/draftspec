using System.Text.Json;
using DraftSpec.Mcp.Resources;

namespace DraftSpec.Tests.Mcp.Resources;

/// <summary>
/// Tests for SpecResources MCP methods.
/// These tests modify the current working directory, so they cannot run in parallel.
/// </summary>
[NotInParallel]
public class SpecResourcesTests
{
    private string _tempDir = null!;
    private string _originalDir = null!;

    [Before(Test)]
    public void SetUp()
    {
        _originalDir = Directory.GetCurrentDirectory();
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
        // Restore original directory before cleanup
        Directory.SetCurrentDirectory(_originalDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region ListSpecs

    [Test]
    public async Task ListSpecs_EmptyDirectory_ReturnsEmptyList()
    {
        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(0);
        await Assert.That(json.RootElement.GetProperty("specs").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task ListSpecs_WithSpecFiles_ReturnsFiles()
    {
        // Create test spec files
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "test.spec.csx"), "// spec content");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "another.spec.csx"), "// another spec");

        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(2);
        await Assert.That(json.RootElement.GetProperty("specs").GetArrayLength()).IsEqualTo(2);
    }

    [Test]
    public async Task ListSpecs_NestedDirectories_FindsAllSpecs()
    {
        // Create nested structure
        var subDir = Path.Combine(_tempDir, "nested");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "root.spec.csx"), "// root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.spec.csx"), "// nested");

        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task ListSpecs_IgnoresNonSpecFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "test.spec.csx"), "// spec");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "other.csx"), "// not a spec");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "readme.md"), "# readme");

        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(1);
    }

    [Test]
    public async Task ListSpecs_NonexistentDirectory_ReturnsError()
    {
        var result = SpecResources.ListSpecs("/nonexistent/path");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    public async Task ListSpecs_IncludesFileMetadata()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(specPath, "// spec content here");

        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        var spec = json.RootElement.GetProperty("specs")[0];
        await Assert.That(spec.GetProperty("name").GetString()).IsEqualTo("test");
        await Assert.That(spec.GetProperty("path").GetString()).IsEqualTo("test.spec.csx");
        await Assert.That(spec.GetProperty("size").GetInt64()).IsGreaterThan(0);
    }

    #endregion

    #region GetSpec

    [Test]
    public async Task GetSpec_ExistingFile_ReturnsContent()
    {
        // CWD is _tempDir from SetUp
        var content = "describe('test', () => { it('works', () => { }); });";
        await File.WriteAllTextAsync("test.spec.csx", content);

        var result = SpecResources.GetSpec("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("content").GetString()).IsEqualTo(content);
        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
    }

    [Test]
    public async Task GetSpec_NonexistentFile_ReturnsError()
    {
        var result = SpecResources.GetSpec("/nonexistent/file.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    public async Task GetSpec_EmptyPath_ReturnsError()
    {
        var result = SpecResources.GetSpec("");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    public async Task GetSpec_NonCsxFile_ReturnsError()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.txt", "not a spec");

        var result = SpecResources.GetSpec("test.txt");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString()).Contains(".csx");
    }

    [Test]
    public async Task GetSpec_RelativePath_Works()
    {
        // CWD is already set to _tempDir by SetUp
        await File.WriteAllTextAsync("test.spec.csx", "// content");

        var result = SpecResources.GetSpec("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("content", out _)).IsTrue();
    }

    [Test]
    public async Task GetSpec_IncludesMetadata()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("mytest.spec.csx", "// spec");

        var result = SpecResources.GetSpec("mytest.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("name").GetString()).IsEqualTo("mytest");
        await Assert.That(json.RootElement.TryGetProperty("size", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("modifiedAt", out _)).IsTrue();
    }

    #endregion

    #region GetSpec Security

    [Test]
    public async Task GetSpec_PathOutsideWorkingDirectory_ReturnsError()
    {
        // CWD is _tempDir from SetUp
        // Create a file in _tempDir
        await File.WriteAllTextAsync("test.spec.csx", "// content");

        // Create a subdirectory and change to it
        Directory.CreateDirectory("subdir");
        Directory.SetCurrentDirectory(Path.Combine(_tempDir, "subdir"));

        // Try to access the parent file using relative path (path traversal)
        var result = SpecResources.GetSpec("../test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString())
            .Contains("within the working directory");
    }

    [Test]
    public async Task GetSpec_AbsolutePathOutsideWorkingDirectory_ReturnsError()
    {
        // CWD is _tempDir from SetUp
        var parentSpecPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(parentSpecPath, "// content");

        // Create a subdirectory and change to it
        Directory.CreateDirectory("subdir");
        Directory.SetCurrentDirectory(Path.Combine(_tempDir, "subdir"));

        // Try to access the parent file using absolute path
        var result = SpecResources.GetSpec(parentSpecPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString())
            .Contains("within the working directory");
    }

    [Test]
    public async Task GetSpec_PathWithinWorkingDirectory_Succeeds()
    {
        // CWD is already _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", "// content");

        var result = SpecResources.GetSpec("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
        await Assert.That(json.RootElement.TryGetProperty("content", out _)).IsTrue();
    }

    #endregion

    #region GetSpecMetadata

    [Test]
    public async Task GetSpecMetadata_ExistingFile_ReturnsMetadata()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", "describe('Test', () => { it('works'); });");

        var result = SpecResources.GetSpecMetadata("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
        await Assert.That(json.RootElement.GetProperty("name").GetString()).IsEqualTo("test");
    }

    [Test]
    public async Task GetSpecMetadata_CountsDescribeBlocks()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", @"
describe('First', () => {
    describe('Nested', () => { });
});
describe('Second', () => { });
");

        var result = SpecResources.GetSpecMetadata("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("stats").GetProperty("describeBlocks").GetInt32()).IsEqualTo(3);
    }

    [Test]
    public async Task GetSpecMetadata_CountsItSpecs()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", @"
describe('Test', () => {
    it('first', () => { });
    it('second', () => { });
    it('third');
});
");

        var result = SpecResources.GetSpecMetadata("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("stats").GetProperty("specs").GetInt32()).IsEqualTo(3);
    }

    [Test]
    public async Task GetSpecMetadata_NonexistentFile_ReturnsError()
    {
        var result = SpecResources.GetSpecMetadata("/nonexistent/file.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    public async Task GetSpecMetadata_EmptyPath_ReturnsError()
    {
        var result = SpecResources.GetSpecMetadata("");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    public async Task GetSpecMetadata_IncludesTimestamps()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", "// spec");

        var result = SpecResources.GetSpecMetadata("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("modifiedAt", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("createdAt", out _)).IsTrue();
    }

    #endregion

    #region GetSpecMetadata Security

    [Test]
    public async Task GetSpecMetadata_PathOutsideWorkingDirectory_ReturnsError()
    {
        // CWD is _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", "// content");

        // Create a subdirectory and change to it
        Directory.CreateDirectory("subdir");
        Directory.SetCurrentDirectory(Path.Combine(_tempDir, "subdir"));

        // Try to access the parent file using relative path (path traversal)
        var result = SpecResources.GetSpecMetadata("../test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString())
            .Contains("within the working directory");
    }

    [Test]
    public async Task GetSpecMetadata_AbsolutePathOutsideWorkingDirectory_ReturnsError()
    {
        // CWD is _tempDir from SetUp
        var parentSpecPath = Path.Combine(_tempDir, "test.spec.csx");
        await File.WriteAllTextAsync(parentSpecPath, "// content");

        // Create a subdirectory and change to it
        Directory.CreateDirectory("subdir");
        Directory.SetCurrentDirectory(Path.Combine(_tempDir, "subdir"));

        // Try to access the parent file using absolute path
        var result = SpecResources.GetSpecMetadata(parentSpecPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString())
            .Contains("within the working directory");
    }

    [Test]
    public async Task GetSpecMetadata_PathWithinWorkingDirectory_Succeeds()
    {
        // CWD is already _tempDir from SetUp
        await File.WriteAllTextAsync("test.spec.csx", "// content");

        var result = SpecResources.GetSpecMetadata("test.spec.csx");
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
        await Assert.That(json.RootElement.TryGetProperty("name", out _)).IsTrue();
    }

    #endregion
}
