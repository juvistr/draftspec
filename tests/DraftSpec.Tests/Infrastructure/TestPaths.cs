namespace DraftSpec.Tests.Infrastructure;

/// <summary>
/// Provides cross-platform path utilities for tests.
/// Use this instead of hardcoded Unix-style paths like "/tmp/" or "/specs/".
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// // Instead of: var path = "/tmp/coverage/coverage.xml";
/// var path = TestPaths.Coverage("coverage.xml");
///
/// // Instead of: var specPath = "/specs/test.spec.csx";
/// var specPath = TestPaths.Spec("test.spec.csx");
///
/// // For assertions comparing paths:
/// await Assert.That(actualPath).IsEqualTo(TestPaths.Coverage("file.xml"));
/// </code>
/// </remarks>
public static class TestPaths
{
    /// <summary>
    /// Gets the system temp directory root (e.g., /tmp on Unix, C:\Users\...\Temp on Windows).
    /// </summary>
    public static string TempRoot => Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

    /// <summary>
    /// Gets a cross-platform path for coverage-related test files.
    /// </summary>
    public static string CoverageDir => Path.Combine(TempRoot, "coverage");

    /// <summary>
    /// Gets a cross-platform path for spec-related test files.
    /// </summary>
    public static string SpecsDir => Path.Combine(TempRoot, "specs");

    /// <summary>
    /// Gets a cross-platform path for schema-related test files.
    /// </summary>
    public static string SchemaDir => TempRoot;

    /// <summary>
    /// Builds a cross-platform path for a coverage file.
    /// </summary>
    /// <param name="fileName">The file name (e.g., "coverage.xml" or "coverage.cobertura.xml")</param>
    /// <returns>Full cross-platform path to the coverage file</returns>
    public static string Coverage(string fileName) => Path.Combine(CoverageDir, fileName);

    /// <summary>
    /// Builds a cross-platform path for a spec file.
    /// </summary>
    /// <param name="relativePath">Relative path within specs dir (e.g., "test.spec.csx" or "nested/spec.csx")</param>
    /// <returns>Full cross-platform path to the spec file</returns>
    public static string Spec(string relativePath) => Path.Combine(SpecsDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Builds a cross-platform path for a file in the temp root.
    /// </summary>
    /// <param name="fileName">The file name (e.g., "schema.json")</param>
    /// <returns>Full cross-platform path to the file</returns>
    public static string Temp(string fileName) => Path.Combine(TempRoot, fileName);

    /// <summary>
    /// Normalizes a path to the current OS format.
    /// Useful for assertions when comparing paths that may have mixed separators.
    /// </summary>
    /// <param name="path">Path to normalize</param>
    /// <returns>Normalized absolute path</returns>
    public static string Normalize(string path) => Path.GetFullPath(path);

    /// <summary>
    /// Creates a nested directory path under specs.
    /// </summary>
    /// <param name="subDirs">Subdirectory names</param>
    /// <returns>Full cross-platform path to the nested specs directory</returns>
    public static string SpecsSubDir(params string[] subDirs) =>
        Path.Combine(new[] { SpecsDir }.Concat(subDirs).ToArray());
}
