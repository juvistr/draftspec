using DraftSpec.Formatters;
using DraftSpec.Formatters.Abstractions;
using DraftSpec.Plugins.Reporters;

namespace DraftSpec.Tests.Plugins.Reporters;

/// <summary>
/// Tests for ConsoleReporter which writes spec reports to the console using an IConsoleFormatter.
/// </summary>
public class ConsoleReporterTests
{
    [Test]
    public async Task Name_ReturnsConsole()
    {
        // Arrange
        var mockFormatter = new MockConsoleFormatter();
        var reporter = new ConsoleReporter(mockFormatter);

        // Act
        var name = reporter.Name;

        // Assert
        await Assert.That(name).IsEqualTo("console");
    }

    [Test]
    public async Task OnRunCompletedAsync_CallsFormatterFormat()
    {
        // Arrange
        var mockFormatter = new MockConsoleFormatter();
        var reporter = new ConsoleReporter(mockFormatter);
        var report = CreateTestReport();

        // Act
        await reporter.OnRunCompletedAsync(report);

        // Assert
        await Assert.That(mockFormatter.FormatCalled).IsTrue();
        await Assert.That(mockFormatter.LastReport).IsSameReferenceAs(report);
        await Assert.That(mockFormatter.LastOutput).IsNotNull();
    }

    [Test]
    public async Task OnRunCompletedAsync_WritesToConsoleOut()
    {
        // Arrange
        var mockFormatter = new MockConsoleFormatter();
        var reporter = new ConsoleReporter(mockFormatter);
        var report = CreateTestReport();

        // Act
        await reporter.OnRunCompletedAsync(report);

        // Assert - The output should be Console.Out
        await Assert.That(mockFormatter.LastOutput).IsSameReferenceAs(Console.Out);
    }

    [Test]
    public void Constructor_NullFormatter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsoleReporter(null!));
    }

    [Test]
    public async Task OnRunCompletedAsync_ReturnsCompletedTask()
    {
        // Arrange
        var mockFormatter = new MockConsoleFormatter();
        var reporter = new ConsoleReporter(mockFormatter);
        var report = CreateTestReport();

        // Act
        var task = reporter.OnRunCompletedAsync(report);

        // Assert
        await Assert.That(task.IsCompleted).IsTrue();
    }

    private static SpecReport CreateTestReport()
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary
            {
                Total = 1,
                Passed = 1,
                Failed = 0,
                Pending = 0,
                Skipped = 0,
                DurationMs = 100
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "test context",
                    Specs =
                    [
                        new SpecResultReport
                        {
                            Description = "test spec",
                            Status = "passed",
                            DurationMs = 100
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// Mock implementation of IConsoleFormatter for testing.
    /// </summary>
    private class MockConsoleFormatter : IConsoleFormatter
    {
        public bool FormatCalled { get; private set; }
        public SpecReport? LastReport { get; private set; }
        public TextWriter? LastOutput { get; private set; }
        public bool LastUseColors { get; private set; }

        public string FileExtension => ".txt";

        public string Format(SpecReport report)
        {
            return "mock output";
        }

        public void Format(SpecReport report, TextWriter output)
        {
            FormatCalled = true;
            LastReport = report;
            LastOutput = output;
        }

        public void Format(SpecReport report, TextWriter output, bool useColors)
        {
            FormatCalled = true;
            LastReport = report;
            LastOutput = output;
            LastUseColors = useColors;
        }
    }
}
