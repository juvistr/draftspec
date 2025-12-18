using System.Text.Json;
using DraftSpec.Mcp.Resources;

namespace DraftSpec.Tests.Mcp.Resources;

/// <summary>
/// Tests for SpecResources MCP methods.
/// </summary>
public class SpecResourcesTests
{
    private string _tempDir = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [After(Test)]
    public void TearDown()
    {
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
        File.WriteAllText(Path.Combine(_tempDir, "test.spec.csx"), "// spec content");
        File.WriteAllText(Path.Combine(_tempDir, "another.spec.csx"), "// another spec");

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
        File.WriteAllText(Path.Combine(_tempDir, "root.spec.csx"), "// root");
        File.WriteAllText(Path.Combine(subDir, "nested.spec.csx"), "// nested");

        var result = SpecResources.ListSpecs(_tempDir);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("count").GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task ListSpecs_IgnoresNonSpecFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.spec.csx"), "// spec");
        File.WriteAllText(Path.Combine(_tempDir, "other.csx"), "// not a spec");
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "# readme");

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
        File.WriteAllText(specPath, "// spec content here");

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
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        var content = "describe('test', () => { it('works', () => { }); });";
        File.WriteAllText(specPath, content);

        var result = SpecResources.GetSpec(specPath);
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
        var txtPath = Path.Combine(_tempDir, "test.txt");
        File.WriteAllText(txtPath, "not a spec");

        var result = SpecResources.GetSpec(txtPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsTrue();
        await Assert.That(json.RootElement.GetProperty("error").GetString()).Contains(".csx");
    }

    [Test]
    public async Task GetSpec_RelativePath_Works()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specPath, "// content");

        // Change to temp directory and use relative path
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir);
            var result = SpecResources.GetSpec("test.spec.csx");
            var json = JsonDocument.Parse(result);

            await Assert.That(json.RootElement.TryGetProperty("content", out _)).IsTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Test]
    public async Task GetSpec_IncludesMetadata()
    {
        var specPath = Path.Combine(_tempDir, "mytest.spec.csx");
        File.WriteAllText(specPath, "// spec");

        var result = SpecResources.GetSpec(specPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("name").GetString()).IsEqualTo("mytest");
        await Assert.That(json.RootElement.TryGetProperty("size", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("modifiedAt", out _)).IsTrue();
    }

    #endregion

    #region GetSpecMetadata

    [Test]
    public async Task GetSpecMetadata_ExistingFile_ReturnsMetadata()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specPath, "describe('Test', () => { it('works'); });");

        var result = SpecResources.GetSpecMetadata(specPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("error", out _)).IsFalse();
        await Assert.That(json.RootElement.GetProperty("name").GetString()).IsEqualTo("test");
    }

    [Test]
    public async Task GetSpecMetadata_CountsDescribeBlocks()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specPath, @"
describe('First', () => {
    describe('Nested', () => { });
});
describe('Second', () => { });
");

        var result = SpecResources.GetSpecMetadata(specPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.GetProperty("stats").GetProperty("describeBlocks").GetInt32()).IsEqualTo(3);
    }

    [Test]
    public async Task GetSpecMetadata_CountsItSpecs()
    {
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specPath, @"
describe('Test', () => {
    it('first', () => { });
    it('second', () => { });
    it('third');
});
");

        var result = SpecResources.GetSpecMetadata(specPath);
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
        var specPath = Path.Combine(_tempDir, "test.spec.csx");
        File.WriteAllText(specPath, "// spec");

        var result = SpecResources.GetSpecMetadata(specPath);
        var json = JsonDocument.Parse(result);

        await Assert.That(json.RootElement.TryGetProperty("modifiedAt", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("createdAt", out _)).IsTrue();
    }

    #endregion
}
