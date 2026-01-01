using System.Security;
using DraftSpec.Cli;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for PathValidator security validation.
/// These tests verify security invariants that must hold for all inputs.
/// </summary>
public class PathValidatorPropertyTests
{
    [Test]
    public void ValidateFileName_RejectsEmptyOrWhitespace()
    {
        // Property: Empty or whitespace-only names are always rejected
        var invalidNames = new[] { "", " ", "  ", "\t", "\n", "   " };

        foreach (var name in invalidNames)
        {
            try
            {
                PathValidator.ValidateFileName(name);
                Assert.Fail($"Expected ArgumentException for name: '{name}'");
            }
            catch (ArgumentException)
            {
                // Expected
            }
        }
    }

    [Test]
    public void ValidateFileName_RejectsPathSeparators()
    {
        // Property: Names containing path separators are always rejected
        Prop.ForAll<NonNull<string>, NonNull<string>>((prefix, suffix) =>
        {
            var separators = new[] { "/", "\\", Path.DirectorySeparatorChar.ToString() };

            foreach (var sep in separators)
            {
                var name = prefix.Get + sep + suffix.Get;
                try
                {
                    PathValidator.ValidateFileName(name);
                    return false; // Should have thrown
                }
                catch (ArgumentException)
                {
                    // Expected
                }
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ValidateFileName_RejectsParentDirectoryReferences()
    {
        // Property: Parent directory references are always rejected
        var parentRefs = new[] { "..", ".", "..foo", "../test" };

        foreach (var name in parentRefs)
        {
            try
            {
                PathValidator.ValidateFileName(name);
                Assert.Fail($"Expected ArgumentException for name: '{name}'");
            }
            catch (ArgumentException)
            {
                // Expected
            }
        }
    }

    [Test]
    public void ValidateFileName_AcceptsValidNames()
    {
        // Property: Simple alphanumeric names with extensions are valid
        Prop.ForAll(
            Gen.Elements("test", "spec", "file", "data", "report")
               .SelectMany(name => Gen.Choose(1, 999)
               .SelectMany(n => Gen.Elements(".txt", ".cs", ".json", ".xml", "")
               .Select(ext => $"{name}_{n}{ext}")))
               .ToArbitrary(),
            name =>
            {
                PathValidator.ValidateFileName(name);
                return true;
            }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ValidatePathWithinBase_AcceptsChildPaths()
    {
        // Property: A direct child path is always valid within its parent
        var tempDir = Path.GetTempPath();
        var baseDir = Path.Combine(tempDir, $"test_base_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(baseDir);

            Prop.ForAll(
                Gen.Elements("sub1", "sub2", "child", "nested")
                   .SelectMany(name => Gen.Choose(1, 100)
                   .Select(n => $"{name}_{n}"))
                   .ToArbitrary(),
                childName =>
                {
                    var childPath = Path.Combine(baseDir, childName);
                    PathValidator.ValidatePathWithinBase(childPath, baseDir);
                    return true;
                }).QuickCheckThrowOnFailure();
        }
        finally
        {
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
        }
    }

    [Test]
    public void ValidatePathWithinBase_RejectsTraversalAttempts()
    {
        // Property: Paths attempting to escape via .. are rejected
        var tempDir = Path.GetTempPath();
        var baseDir = Path.Combine(tempDir, $"test_base_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(baseDir);

            var traversalPaths = new[]
            {
                Path.Combine(baseDir, "..", "escape"),
                Path.Combine(baseDir, "sub", "..", "..", "escape"),
                Path.Combine(baseDir, ".", "..", "escape"),
            };

            foreach (var path in traversalPaths)
            {
                try
                {
                    PathValidator.ValidatePathWithinBase(path, baseDir);
                    Assert.Fail($"Expected SecurityException for path: {path}");
                }
                catch (SecurityException)
                {
                    // Expected
                }
            }
        }
        finally
        {
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
        }
    }

    [Test]
    public void ValidatePathWithinBase_RejectsPrefixAttacks()
    {
        // Property: /base/evil should NOT pass for base /base
        // This tests the trailing separator protection
        var tempDir = Path.GetTempPath();
        var baseName = $"test_base_{Guid.NewGuid():N}";
        var baseDir = Path.Combine(tempDir, baseName);
        var evilDir = Path.Combine(tempDir, baseName + "-evil");

        try
        {
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(evilDir);

            // The evil path should NOT be within baseDir
            try
            {
                PathValidator.ValidatePathWithinBase(evilDir, baseDir);
                Assert.Fail("Expected SecurityException for prefix attack");
            }
            catch (SecurityException)
            {
                // Expected - prefix attack was blocked
            }
        }
        finally
        {
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
            if (Directory.Exists(evilDir))
                Directory.Delete(evilDir, recursive: true);
        }
    }

    [Test]
    public void ValidatePathWithinBase_SamePathIsValid()
    {
        // Property: A path is always within itself
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, $"test_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(testDir);
            PathValidator.ValidatePathWithinBase(testDir, testDir);
        }
        finally
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, recursive: true);
        }
    }

    [Test]
    public void ValidatePathWithinBase_NestedPathsAreValid()
    {
        // Property: Deeply nested paths within base are valid
        var tempDir = Path.GetTempPath();
        var baseDir = Path.Combine(tempDir, $"test_base_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(baseDir);

            Prop.ForAll(Gen.Choose(1, 5).ToArbitrary(), depth =>
            {
                var parts = Enumerable.Range(0, depth).Select(i => $"level{i}").ToArray();
                var nestedPath = Path.Combine(new[] { baseDir }.Concat(parts).ToArray());

                PathValidator.ValidatePathWithinBase(nestedPath, baseDir);
                return true;
            }).QuickCheckThrowOnFailure();
        }
        finally
        {
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
        }
    }
}
