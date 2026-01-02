using DraftSpec.Cli;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for SystemConsole wrapper.
/// Runs sequentially since Console.SetOut affects global state.
/// </summary>
[NotInParallel]
public class SystemConsoleTests
{
    #region Write Methods - Verify No Exceptions

    [Test]
    public async Task Write_DoesNotThrow()
    {
        var console = new SystemConsole();

        // Just verify it doesn't throw - actual output goes to real console
        console.Write("");

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task WriteLine_DoesNotThrow()
    {
        var console = new SystemConsole();

        console.WriteLine("");

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task WriteLine_NoArgs_DoesNotThrow()
    {
        var console = new SystemConsole();

        console.WriteLine();

        await Assert.That(true).IsTrue();
    }

    #endregion

    #region Color Methods

    [Test]
    public async Task ForegroundColor_GetSet_Works()
    {
        // Skip on CI - console color may not work without a real terminal
        if (Environment.GetEnvironmentVariable("CI") == "true") return;

        var console = new SystemConsole();
        var original = console.ForegroundColor;

        console.ForegroundColor = ConsoleColor.Cyan;
        var result = console.ForegroundColor;

        console.ForegroundColor = original; // Restore
        await Assert.That(result).IsEqualTo(ConsoleColor.Cyan);
    }

    [Test]
    public async Task ResetColor_DoesNotThrow()
    {
        var console = new SystemConsole();
        console.ForegroundColor = ConsoleColor.Red;

        console.ResetColor();

        await Assert.That(true).IsTrue(); // Just verify no exception
    }

    #endregion

    #region Styled Output Methods

    [Test]
    public async Task WriteWarning_OutputsText()
    {
        var console = new SystemConsole();
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);
            console.WriteWarning("warning message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        await Assert.That(capturedOut.ToString()).Contains("warning message");
    }

    [Test]
    public async Task WriteSuccess_OutputsText()
    {
        var console = new SystemConsole();
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);
            console.WriteSuccess("success message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        await Assert.That(capturedOut.ToString()).Contains("success message");
    }

    [Test]
    public async Task WriteError_OutputsText()
    {
        var console = new SystemConsole();
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);
            console.WriteError("error message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        await Assert.That(capturedOut.ToString()).Contains("error message");
    }

    #endregion
}
