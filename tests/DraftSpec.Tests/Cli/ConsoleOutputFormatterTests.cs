using DraftSpec.Cli.Services;
using DraftSpec.Formatters;

namespace DraftSpec.Tests.Cli;

/// <summary>
/// Tests for ConsoleOutputFormatter.
/// </summary>
public class ConsoleOutputFormatterTests
{
    #region Basic Formatting

    [Test]
    public async Task Format_EmptyReport_ReturnsEmptyString()
    {
        var report = new SpecReport();

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_SingleContext_FormatsDescription()
    {
        var report = new SpecReport
        {
            Contexts = [new SpecContextReport { Description = "UserService" }]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).IsEqualTo("UserService");
    }

    #endregion

    #region Status Symbols

    [Test]
    public async Task Format_PassedSpec_ShowsCheckmark()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "adds numbers", Status = "passed" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("\u2713 adds numbers");
    }

    [Test]
    public async Task Format_FailedSpec_ShowsX()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "divides by zero", Status = "failed" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("\u2717 divides by zero");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsCircle()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "multiplies matrices", Status = "pending" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("\u25cb multiplies matrices");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsDash()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "slow test", Status = "skipped" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("- slow test");
    }

    [Test]
    public async Task Format_UnknownStatus_ShowsQuestionMark()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "weird test", Status = "unknown" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("? weird test");
    }

    #endregion

    #region Error Messages

    [Test]
    public async Task Format_FailedSpecWithError_ShowsErrorMessage()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs =
                    [
                        new SpecResultReport
                        {
                            Description = "validates input",
                            Status = "failed",
                            Error = "Expected 42 but got 0"
                        }
                    ]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("Expected 42 but got 0");
    }

    [Test]
    public async Task Format_SpecWithNullError_DoesNotShowError()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "works", Status = "passed", Error = null }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);
        var lines = result.Split(Environment.NewLine);

        // Should only have 2 lines: context description and spec
        await Assert.That(lines.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Format_SpecWithEmptyError_DoesNotShowError()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Calculator",
                    Specs = [new SpecResultReport { Description = "works", Status = "passed", Error = "" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);
        var lines = result.Split(Environment.NewLine);

        await Assert.That(lines.Length).IsEqualTo(2);
    }

    #endregion

    #region Indentation

    [Test]
    public async Task Format_NestedContext_IndentsCorrectly()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "UserService",
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "when creating a user",
                            Specs = [new SpecResultReport { Description = "saves to database", Status = "passed" }]
                        }
                    ]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);
        var lines = result.Split(Environment.NewLine);

        await Assert.That(lines[0]).IsEqualTo("UserService");
        await Assert.That(lines[1]).IsEqualTo("  when creating a user");
        await Assert.That(lines[2]).StartsWith("    \u2713");
    }

    [Test]
    public async Task Format_DeeplyNested_IndentsMultipleLevels()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Level 0",
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "Level 1",
                            Contexts =
                            [
                                new SpecContextReport
                                {
                                    Description = "Level 2",
                                    Specs = [new SpecResultReport { Description = "deep spec", Status = "passed" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);
        var lines = result.Split(Environment.NewLine);

        await Assert.That(lines[0]).IsEqualTo("Level 0");
        await Assert.That(lines[1]).IsEqualTo("  Level 1");
        await Assert.That(lines[2]).IsEqualTo("    Level 2");
        await Assert.That(lines[3]).StartsWith("      \u2713");
    }

    #endregion

    #region Multiple Specs and Contexts

    [Test]
    public async Task Format_MultipleSpecs_FormatsAll()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Math",
                    Specs =
                    [
                        new SpecResultReport { Description = "adds", Status = "passed" },
                        new SpecResultReport { Description = "subtracts", Status = "passed" },
                        new SpecResultReport { Description = "divides", Status = "failed" }
                    ]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("\u2713 adds");
        await Assert.That(result).Contains("\u2713 subtracts");
        await Assert.That(result).Contains("\u2717 divides");
    }

    [Test]
    public async Task Format_MultipleTopLevelContexts_FormatsAll()
    {
        var report = new SpecReport
        {
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "UserService",
                    Specs = [new SpecResultReport { Description = "creates user", Status = "passed" }]
                },
                new SpecContextReport
                {
                    Description = "OrderService",
                    Specs = [new SpecResultReport { Description = "creates order", Status = "passed" }]
                }
            ]
        };

        var result = ConsoleOutputFormatter.Format(report);

        await Assert.That(result).Contains("UserService");
        await Assert.That(result).Contains("OrderService");
        await Assert.That(result).Contains("creates user");
        await Assert.That(result).Contains("creates order");
    }

    #endregion
}
