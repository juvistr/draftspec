using System.Collections.Immutable;
using DraftSpec.Cli;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for CompilationDiagnosticFormatter class.
/// </summary>
public class CompilationDiagnosticFormatterTests
{
    #region Format Tests

    [Test]
    public async Task Format_WithCompilationError_IncludesErrorCode()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        await Assert.That(result).Contains("CS");
    }

    [Test]
    public async Task Format_WithCompilationError_IncludesErrorMessage()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Error message should be included (specific message varies by error)
        await Assert.That(result.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Format_WithColors_IncludesAnsiCodes()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: true);

        await Assert.That(result).Contains(AnsiColors.Red);
    }

    [Test]
    public async Task Format_WithoutColors_OmitsAnsiCodes()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        await Assert.That(result).DoesNotContain("\x1b[");
    }

    #endregion

    #region Line Information Tests

    [Test]
    public async Task Format_WithCompilationError_IncludesLineNumber()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Should include "Line X" from diagnostic location
        await Assert.That(result).Contains("Line");
    }

    [Test]
    public async Task Format_WithCompilationError_IncludesColumnNumber()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Should include "Column X" from diagnostic location
        await Assert.That(result).Contains("Column");
    }

    [Test]
    public async Task Format_WhenSourceFileNotAvailable_StillFormatsError()
    {
        // When the source file isn't available (file path doesn't match filesystem),
        // the formatter should still show the error message without source context
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem(); // No files registered
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Should still contain error code and line info even without source context
        await Assert.That(result).Contains("CS");
        await Assert.That(result).Contains("Line");
    }

    #endregion

    #region FormatDiagnostic Tests

    [Test]
    public async Task FormatDiagnostic_SingleDiagnostic_FormatsCorrectly()
    {
        var diagnostic = CreateDiagnostic("var x = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        await Assert.That(result.Length).IsGreaterThan(0);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Format_WithMissingSourceFile_StillFormatsError()
    {
        var exception = CreateCompilationError("var x = ");
        var mockFileSystem = new MockFileSystem(); // No files
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Should still format the error message without source context
        await Assert.That(result).Contains("CS");
    }

    [Test]
    public async Task Format_MultipleErrors_FormatsAll()
    {
        var exception = CreateCompilationError("var x = \nvar y = ");
        var mockFileSystem = new MockFileSystem();
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.Format(exception, useColors: false);

        // Should contain error information (at least one error)
        await Assert.That(result).Contains("CS");
    }

    #endregion

    #region Helper Methods

    private static CompilationErrorException CreateCompilationError(string code)
    {
        try
        {
            var script = CSharpScript.Create(code);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                return new CompilationErrorException(
                    "Compilation failed",
                    diagnostics.ToImmutableArray());
            }

            // Force evaluate to get runtime compilation errors
            script.RunAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException("Expected compilation error");
        }
        catch (CompilationErrorException ex)
        {
            return ex;
        }
    }

    private static CompilationErrorException CreateCompilationErrorFromCode(string code)
    {
        try
        {
            var script = CSharpScript.Create(code, ScriptOptions.Default);
            script.RunAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException("Expected compilation error");
        }
        catch (CompilationErrorException ex)
        {
            return ex;
        }
    }

    private static Diagnostic CreateDiagnostic(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(syntaxTree);

        return compilation.GetDiagnostics()
            .First(d => d.Severity == DiagnosticSeverity.Error);
    }

    #endregion

    #region Mock Implementations

    private class MockFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

        public MockFileSystem WithFile(string path, string content)
        {
            _files[path] = content;
            return this;
        }

        public bool FileExists(string path) => _files.ContainsKey(path);

        public string ReadAllText(string path) =>
            _files.TryGetValue(path, out var content) ? content : throw new FileNotFoundException(path);

        public void WriteAllText(string path, string content) => _files[path] = content;

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        {
            _files[path] = content;
            return Task.CompletedTask;
        }

        public bool DirectoryExists(string path) => true;
        public void CreateDirectory(string path) { }
        public string[] GetFiles(string path, string searchPattern) => [];
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];
        public DateTime GetLastWriteTimeUtc(string path) => DateTime.MinValue;
    }

    #endregion
}
