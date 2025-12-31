namespace DraftSpec.Tests.Core;

/// <summary>
/// Tests for custom exception types.
/// </summary>
public class ExceptionTests
{
    #region CompilationException

    [Test]
    public async Task CompilationException_WithMessage_SetsMessage()
    {
        var exception = new CompilationException("Syntax error at line 5");

        await Assert.That(exception.Message).IsEqualTo("Syntax error at line 5");
        await Assert.That(exception.InnerException).IsNull();
    }

    [Test]
    public async Task CompilationException_WithInnerException_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("Parse failed");
        var exception = new CompilationException("Compilation failed", inner);

        await Assert.That(exception.Message).IsEqualTo("Compilation failed");
        await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
    }

    [Test]
    public async Task CompilationException_IsException()
    {
        var exception = new CompilationException("test");

        await Assert.That(exception).IsAssignableTo<Exception>();
    }

    #endregion

    #region SpecExecutionException

    [Test]
    public async Task SpecExecutionException_WithMessage_SetsMessage()
    {
        var exception = new SpecExecutionException("Spec body threw");

        await Assert.That(exception.Message).IsEqualTo("Spec body threw");
        await Assert.That(exception.InnerException).IsNull();
    }

    [Test]
    public async Task SpecExecutionException_WithInnerException_SetsMessageAndInner()
    {
        var inner = new NullReferenceException("Object was null");
        var exception = new SpecExecutionException("Execution failed", inner);

        await Assert.That(exception.Message).IsEqualTo("Execution failed");
        await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
    }

    [Test]
    public async Task SpecExecutionException_IsException()
    {
        var exception = new SpecExecutionException("test");

        await Assert.That(exception).IsAssignableTo<Exception>();
    }

    #endregion
}
