using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for ErrorParser.
/// </summary>
public class ErrorParserTests
{
    #region No Error Cases

    [Test]
    public async Task Parse_SuccessfulExecution_ReturnsNull()
    {
        var result = ErrorParser.Parse(stderr: null, stdout: "output", exitCode: 0, timedOut: false);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Parse_EmptyStderrWithSuccessExitCode_ReturnsNull()
    {
        var result = ErrorParser.Parse(stderr: "", stdout: "output", exitCode: 0, timedOut: false);

        await Assert.That(result).IsNull();
    }

    #endregion

    #region Timeout Errors

    [Test]
    public async Task Parse_TimedOut_ReturnsTimeoutCategory()
    {
        var result = ErrorParser.Parse(stderr: null, stdout: null, exitCode: -1, timedOut: true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Timeout);
        await Assert.That(result.Message).Contains("timed out");
    }

    [Test]
    public async Task Parse_TimedOut_TakesPrecedenceOverOtherErrors()
    {
        var stderr = "error CS1002: ; expected";
        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: true);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Timeout);
    }

    #endregion

    #region Compilation Errors

    [Test]
    public async Task Parse_CompilationError_ReturnsCompilationCategory()
    {
        var stderr = "/tmp/spec.cs(5,10): error CS1002: ; expected";

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Compilation);
    }

    [Test]
    public async Task Parse_CompilationError_ExtractsMessage()
    {
        var stderr = "/tmp/spec.cs(5,10): error CS1002: ; expected";

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Message).IsEqualTo("; expected");
    }

    [Test]
    public async Task Parse_CompilationError_ExtractsLineNumber()
    {
        var stderr = "/tmp/spec.cs(42,15): error CS1002: ; expected";

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.LineNumber).IsEqualTo(42);
        await Assert.That(result.ColumnNumber).IsEqualTo(15);
    }

    [Test]
    public async Task Parse_CompilationError_ExtractsErrorCode()
    {
        var stderr = "/tmp/spec.cs(5,10): error CS0103: The name 'foo' does not exist";

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.ErrorCode).IsEqualTo("CS0103");
    }

    [Test]
    public async Task Parse_CompilationError_ExtractsSourceFile()
    {
        var stderr = "/tmp/my-spec.cs(5,10): error CS1002: ; expected";

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.SourceFile).IsEqualTo("/tmp/my-spec.cs");
    }

    #endregion

    #region Assertion Errors

    [Test]
    public async Task Parse_AssertionError_ReturnsAssertionCategory()
    {
        var output = """
            DraftSpec.AssertionException: Expected result to be 5, but was 3
               at Program.<>c.<<Main>$>b__0_0() in /tmp/spec.cs:line 10
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Assertion);
    }

    [Test]
    public async Task Parse_AssertionError_ExtractsExpectedAndActual()
    {
        var output = """
            DraftSpec.AssertionException: Expected result to be "hello", but was "world"
               at Program.<>c.<<Main>$>b__0_0() in /tmp/spec.cs:line 10
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.ExpectedValue).IsEqualTo("\"hello\"");
        await Assert.That(result.ActualValue).IsEqualTo("\"world\"");
    }

    [Test]
    public async Task Parse_AssertionError_ExtractsLineNumber()
    {
        var output = """
            DraftSpec.AssertionException: Expected x to be 5, but was 3
               at Program.<>c.<<Main>$>b__0_0() in /tmp/spec.cs:line 42
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.LineNumber).IsEqualTo(42);
    }

    [Test]
    public async Task Parse_AssertionError_HandlesNumericValues()
    {
        var output = "Expected count to be 10, but was 5";

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Assertion);
        await Assert.That(result.ExpectedValue).IsEqualTo("10");
        await Assert.That(result.ActualValue).IsEqualTo("5");
    }

    [Test]
    public async Task Parse_AssertionError_InStdout_IsDetected()
    {
        var stdout = "Expected value to be true, but was false";

        var result = ErrorParser.Parse(stderr: null, stdout: stdout, exitCode: 1, timedOut: false);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Assertion);
    }

    #endregion

    #region Setup Errors

    [Test]
    public async Task Parse_SetupError_BeforeAll_ReturnsSetupCategory()
    {
        var output = """
            System.InvalidOperationException: Database connection failed
               at beforeAll hook
               at Program.<>c.<<Main>$>b__0_0() in /tmp/spec.cs:line 5
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Setup);
    }

    [Test]
    public async Task Parse_SetupError_Before_ReturnsSetupCategory()
    {
        var output = """
            Exception in before( hook
            System.NullReferenceException: Object reference not set
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Setup);
    }

    #endregion

    #region Teardown Errors

    [Test]
    public async Task Parse_TeardownError_AfterAll_ReturnsTeardownCategory()
    {
        var output = """
            System.InvalidOperationException: Cleanup failed
               at afterAll hook
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Teardown);
    }

    [Test]
    public async Task Parse_TeardownError_After_ReturnsTeardownCategory()
    {
        var output = """
            Exception in after( hook
            System.ObjectDisposedException: Cannot access disposed object
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Teardown);
    }

    #endregion

    #region Runtime Errors

    [Test]
    public async Task Parse_RuntimeError_ReturnsRuntimeCategory()
    {
        var stderr = """
            System.NullReferenceException: Object reference not set to an instance of an object
               at Program.<>c.<<Main>$>b__0_0() in /tmp/spec.cs:line 15
            """;

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Runtime);
    }

    [Test]
    public async Task Parse_RuntimeError_ExtractsExceptionMessage()
    {
        var stderr = """
            System.ArgumentException: Value cannot be negative
               at MyMethod() in /tmp/spec.cs:line 20
            """;

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Message).IsEqualTo("Value cannot be negative");
    }

    [Test]
    public async Task Parse_RuntimeError_ExtractsStackTrace()
    {
        var stderr = """
            System.Exception: Something went wrong
               at Foo.Bar() in /tmp/spec.cs:line 10
               at Program.Main() in /tmp/spec.cs:line 5
            """;

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.StackTrace).IsNotNull();
        await Assert.That(result.StackTrace).Contains("at Foo.Bar()");
    }

    [Test]
    public async Task Parse_NonZeroExitCode_WithNoOutput_ReturnsRuntimeError()
    {
        var result = ErrorParser.Parse(stderr: null, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Runtime);
        await Assert.That(result.Message).Contains("exit");
    }

    #endregion

    #region Configuration Errors

    [Test]
    public async Task Parse_ConfigurationError_InvalidSpec_ReturnsConfigurationCategory()
    {
        var output = "Invalid spec: describe cannot be nested inside it block";

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Configuration);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Parse_MultipleErrors_ReturnsFirstRelevantError()
    {
        // Compilation error should take precedence over generic errors
        var stderr = """
            Some warning message
            /tmp/spec.cs(5,10): error CS1002: ; expected
            Another message
            """;

        var result = ErrorParser.Parse(stderr, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Category).IsEqualTo(ErrorCategory.Compilation);
    }

    [Test]
    public async Task Parse_ComplexAssertionMessage_ExtractsCorrectly()
    {
        var output = """
            DraftSpec.AssertionException: Expected user.Name to be "John Doe", but was "Jane Doe"
            """;

        var result = ErrorParser.Parse(stderr: output, stdout: null, exitCode: 1, timedOut: false);

        await Assert.That(result!.Message).Contains("user.Name");
    }

    #endregion
}
