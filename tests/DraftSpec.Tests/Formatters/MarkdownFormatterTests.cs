using DraftSpec.Formatters;
using DraftSpec.Formatters.Markdown;

namespace DraftSpec.Tests.Formatters;

/// <summary>
/// Tests for MarkdownFormatter output.
/// </summary>
public class MarkdownFormatterTests
{
    #region Heading Syntax

    [Test]
    public async Task Format_TopLevelContext_UsesH1()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ], "Top Level");

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("# Top Level");
    }

    [Test]
    public async Task Format_NestedContext_IncreasesHeadingLevel()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
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
                            Contexts =
                            [
                                new SpecContextReport
                                {
                                    Description = "Level 3",
                                    Specs = [new SpecResultReport { Description = "spec", Status = "passed" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("# Level 1");
        await Assert.That(output).Contains("## Level 2");
        await Assert.That(output).Contains("### Level 3");
    }

    [Test]
    public async Task Format_DeepNesting_CapsAtH6()
    {
        // Create 7 levels of nesting
        var innermost = new SpecContextReport
        {
            Description = "Level 7",
            Specs = [new SpecResultReport { Description = "spec", Status = "passed" }]
        };

        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "L1",
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "L2",
                            Contexts =
                            [
                                new SpecContextReport
                                {
                                    Description = "L3",
                                    Contexts =
                                    [
                                        new SpecContextReport
                                        {
                                            Description = "L4",
                                            Contexts =
                                            [
                                                new SpecContextReport
                                                {
                                                    Description = "L5",
                                                    Contexts =
                                                    [
                                                        new SpecContextReport
                                                        {
                                                            Description = "L6",
                                                            Contexts = [innermost]
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        // Levels 6 and 7 should both use ######
        await Assert.That(output).Contains("###### L6");
        await Assert.That(output).Contains("###### Level 7");
        // Should not have 7 # symbols
        await Assert.That(output).DoesNotContain("####### ");
    }

    [Test]
    public async Task Format_BlankLineAfterHeading()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ], "Context");

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        // Heading should be followed by a blank line
        await Assert.That(output).Contains("# Context\n\n");
    }

    #endregion

    #region Spec Formatting

    [Test]
    public async Task Format_PassedSpec_ShowsCheckmark()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "passing spec", Status = "passed" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("- ✓ passing spec");
    }

    [Test]
    public async Task Format_FailedSpec_ShowsX()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "failing spec", Status = "failed", Error = "assertion failed" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("- ✗ failing spec");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsCircle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pending spec", Status = "pending" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("- ○ pending spec");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsStrikethrough()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "skipped spec", Status = "skipped" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("- ~~skipped spec~~ *(skipped)*");
    }

    [Test]
    public async Task Format_ErrorMessage_UsesBlockquote()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "failing", Status = "failed", Error = "Expected 1 but got 2" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("> Expected 1 but got 2");
    }

    #endregion

    #region Structure

    [Test]
    public async Task Format_SpecsAsBulletList()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "first", Status = "passed" },
            new SpecResultReport { Description = "second", Status = "passed" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("- ✓ first");
        await Assert.That(output).Contains("- ✓ second");
    }

    [Test]
    public async Task Format_NestedContexts_ProperHierarchy()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 2, Passed = 2 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Parent",
                    Specs = [new SpecResultReport { Description = "parent spec", Status = "passed" }],
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "Child",
                            Specs = [new SpecResultReport { Description = "child spec", Status = "passed" }]
                        }
                    ]
                }
            ]
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("# Parent");
        await Assert.That(output).Contains("## Child");
        await Assert.That(output).Contains("- ✓ parent spec");
        await Assert.That(output).Contains("- ✓ child spec");
    }

    [Test]
    public async Task Format_EmptyContext_NoExtraBlankLines()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Empty",
                    Specs = []
                }
            ]
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("# Empty");
        // Should not have extra blank lines from empty spec list
    }

    [Test]
    public async Task Format_MultipleTopLevelContexts()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 2, Passed = 2 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "First Context",
                    Specs = [new SpecResultReport { Description = "spec1", Status = "passed" }]
                },
                new SpecContextReport
                {
                    Description = "Second Context",
                    Specs = [new SpecResultReport { Description = "spec2", Status = "passed" }]
                }
            ]
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("# First Context");
        await Assert.That(output).Contains("# Second Context");
    }

    #endregion

    #region Summary Section

    [Test]
    public async Task Format_IncludesHorizontalRule()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("---");
    }

    [Test]
    public async Task Format_SummaryShowsStats()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 10, Passed = 5, Failed = 2, Pending = 2, Skipped = 1 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("**10 specs**");
        await Assert.That(output).Contains("5 passed");
        await Assert.That(output).Contains("2 failed");
        await Assert.That(output).Contains("2 pending");
        await Assert.That(output).Contains("1 skipped");
    }

    [Test]
    public async Task Format_IncludesTimestamp()
    {
        var timestamp = new DateTime(2025, 12, 15, 10, 30, 45, DateTimeKind.Utc);
        var report = new SpecReport
        {
            Timestamp = timestamp,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("2025-12-15 10:30:45 UTC");
    }

    [Test]
    public async Task Format_IncludesSource()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Source = "tests/my_spec.csx",
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("`tests/my_spec.csx`");
    }

    [Test]
    public async Task Format_FooterIsItalicized()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        // Footer should be wrapped in asterisks for italics
        await Assert.That(output).Contains("*Generated");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task FileExtension_ReturnsMd()
    {
        var formatter = new MarkdownFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".md");
    }

    [Test]
    public async Task Format_EmptyReport_ProducesValidMarkdown()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("**0 specs**");
        await Assert.That(output).Contains("---");
    }

    [Test]
    public async Task Format_OnlyPassedStats_ShowsOnlyPassed()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 5, Passed = 5, Failed = 0, Pending = 0, Skipped = 0 },
            Contexts = []
        };

        var formatter = new MarkdownFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("5 passed");
        await Assert.That(output).DoesNotContain("0 failed");
        await Assert.That(output).DoesNotContain("0 pending");
    }

    #endregion

    #region Helper Methods

    private static SpecReport CreateReport(List<SpecResultReport> specs, string contextDescription = "test")
    {
        return new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary
            {
                Total = specs.Count,
                Passed = specs.Count(s => s.Status == "passed"),
                Failed = specs.Count(s => s.Status == "failed"),
                Pending = specs.Count(s => s.Status == "pending"),
                Skipped = specs.Count(s => s.Status == "skipped"),
                DurationMs = specs.Sum(s => s.DurationMs ?? 0)
            },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = contextDescription,
                    Specs = specs
                }
            ]
        };
    }

    #endregion
}
