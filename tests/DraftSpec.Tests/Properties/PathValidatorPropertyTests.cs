using System.Security;
using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;
using FsCheck;
using FsCheck.Fluent;

namespace DraftSpec.Tests.Properties;

/// <summary>
/// Property-based tests for PathValidator security validation.
/// These tests verify security invariants that must hold for all inputs.
/// </summary>
public class PathValidatorPropertyTests
{
    private static PathValidator CreateValidator()
    {
        var os = new MockOperatingSystem();
        var pathComparer = new SystemPathComparer(os);
        return new PathValidator(pathComparer);
    }

    [Test]
    public void ValidateFileName_RejectsEmptyOrWhitespace()
    {
        var validator = CreateValidator();

        // Property: Empty or whitespace-only names are always rejected
        var invalidNames = new[] { "", " ", "  ", "\t", "\n", "   " };

        foreach (var name in invalidNames)
        {
            try
            {
                validator.ValidateFileName(name);
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
        var validator = CreateValidator();

        // Property: Names containing path separators are always rejected
        Prop.ForAll<NonNull<string>, NonNull<string>>((prefix, suffix) =>
        {
            var separators = new[] { "/", "\\", Path.DirectorySeparatorChar.ToString() };

            foreach (var sep in separators)
            {
                var name = prefix.Get + sep + suffix.Get;
                try
                {
                    validator.ValidateFileName(name);
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
        var validator = CreateValidator();

        // Property: Parent directory references are always rejected
        var parentRefs = new[] { "..", ".", "..foo", "../test" };

        foreach (var name in parentRefs)
        {
            try
            {
                validator.ValidateFileName(name);
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
        var validator = CreateValidator();

        // Property: Simple alphanumeric names with extensions are valid
        Prop.ForAll(
            Gen.Elements("test", "spec", "file", "data", "report")
               .SelectMany(name => Gen.Choose(1, 999)
               .SelectMany(n => Gen.Elements(".txt", ".cs", ".json", ".xml", "")
               .Select(ext => $"{name}_{n}{ext}")))
               .ToArbitrary(),
            name =>
            {
                validator.ValidateFileName(name);
                return true;
            }).QuickCheckThrowOnFailure();
    }

    [Test]
    public void ValidatePathWithinBase_AcceptsChildPaths()
    {
        var validator = CreateValidator();

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
                    validator.ValidatePathWithinBase(childPath, baseDir);
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
        var validator = CreateValidator();

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
                    validator.ValidatePathWithinBase(path, baseDir);
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
        var validator = CreateValidator();

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
                validator.ValidatePathWithinBase(evilDir, baseDir);
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
        var validator = CreateValidator();

        // Property: A path is always within itself
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, $"test_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(testDir);
            validator.ValidatePathWithinBase(testDir, testDir);
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
        var validator = CreateValidator();

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

                validator.ValidatePathWithinBase(nestedPath, baseDir);
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
