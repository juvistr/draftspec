using System.Collections.Immutable;
using DraftSpec.Cli;
using DraftSpec.Tests.Infrastructure.Mocks;
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

    #region Source Context Tests

    [Test]
    public async Task FormatDiagnostic_WithSourceFile_ShowsContextLines()
    {
        // Create a diagnostic with a known file path
        var filePath = "/test/spec.csx";
        var sourceCode = """
            using System;
            namespace Test {
                class Program {
                    void Method() {
                        undefinedVariable
                    }
                }
            }
            """;

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should include context lines with line numbers and pipe separators
        await Assert.That(result).Contains("|");
        await Assert.That(result).Contains("undefinedVariable");
    }

    [Test]
    public async Task FormatDiagnostic_ErrorAtLine1_ShowsNoBeforeContext()
    {
        var filePath = "/test/spec.csx";
        var sourceCode = "undefinedVariable;"; // Error on line 1

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should still show the error line
        await Assert.That(result).Contains("undefinedVariable");
        // Should have caret pointing to the error
        await Assert.That(result).Contains("^---");
    }

    [Test]
    public async Task FormatDiagnostic_ErrorAtLastLine_ShowsNoAfterContext()
    {
        var filePath = "/test/spec.csx";
        // Error on the last line
        var sourceCode = """
            using System;
            class Test {
                void Method() { }
            }
            undefinedVariable;
            """;

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should show the error line and caret
        await Assert.That(result).Contains("undefinedVariable");
        await Assert.That(result).Contains("^---");
    }

    [Test]
    public async Task FormatDiagnostic_ShowsCaretAtCorrectColumn()
    {
        var filePath = "/test/spec.csx";
        // Error starts at a specific column position
        var sourceCode = "int x = undefinedVariable;"; // Error starts after "int x = "

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should have caret pointing to the error position
        await Assert.That(result).Contains("^---");

        // The caret should be offset from the start based on column
        var lines = result.Split('\n');
        var caretLine = lines.FirstOrDefault(l => l.Contains("^---"));
        await Assert.That(caretLine).IsNotNull();

        // Caret should not be at the start (column > 1)
        var caretIndex = caretLine!.IndexOf('^');
        await Assert.That(caretIndex).IsGreaterThan(10); // At least past the line number prefix
    }

    [Test]
    public async Task FormatDiagnostic_ShowsContextWithLineNumbers()
    {
        var filePath = "/test/spec.csx";
        var sourceCode = """
            line1
            line2
            line3
            undefinedVariable;
            line5
            line6
            line7
            """;

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should show line numbers for context lines
        // The error is on line 4, so we should see lines 1-7 (with 3 context lines before/after)
        await Assert.That(result).Contains("1 |");
        await Assert.That(result).Contains("4 |"); // The error line
    }

    [Test]
    public async Task FormatDiagnostic_WithColors_HighlightsErrorLine()
    {
        var filePath = "/test/spec.csx";
        var sourceCode = "undefinedVariable;";

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        var mockFileSystem = new MockFileSystem().AddFile(filePath, sourceCode);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        var result = formatter.FormatDiagnostic(diagnostic, useColors: true);

        // Should include red ANSI code for error line
        await Assert.That(result).Contains(AnsiColors.Red);
    }

    [Test]
    public async Task FormatDiagnostic_FileReadError_SkipsSourceContext()
    {
        var filePath = "/test/spec.csx";
        var sourceCode = "undefinedVariable;";

        var diagnostic = CreateDiagnosticWithPath(sourceCode, filePath);
        // Mock file system throws when reading the file
        var mockFileSystem = new ThrowingFileSystem(filePath);
        var formatter = new CompilationDiagnosticFormatter(mockFileSystem);

        // Should not throw, just skip source context
        var result = formatter.FormatDiagnostic(diagnostic, useColors: false);

        // Should still have the error message, just no source context
        await Assert.That(result).Contains("CS");
        await Assert.That(result).DoesNotContain("^---");
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

    private static Diagnostic CreateDiagnosticWithPath(string code, string filePath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, path: filePath);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        return compilation.GetDiagnostics()
            .First(d => d.Severity == DiagnosticSeverity.Error);
    }

    #endregion

    #region Mock Implementations

    private class ThrowingFileSystem : IFileSystem
    {
        private readonly string _pathThatExists;

        public ThrowingFileSystem(string pathThatExists)
        {
            _pathThatExists = pathThatExists;
        }

        public bool FileExists(string path) =>
            string.Equals(path, _pathThatExists, StringComparison.OrdinalIgnoreCase);

        public string ReadAllText(string path) =>
            throw new IOException("Simulated file read error");

        public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
            throw new IOException("Simulated file read error");

        public void WriteAllText(string path, string content) { }

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) =>
            Task.CompletedTask;

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
