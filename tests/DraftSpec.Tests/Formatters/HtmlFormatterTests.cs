using DraftSpec.Formatters;
using DraftSpec.Formatters.Html;

namespace DraftSpec.Tests.Formatters;

/// <summary>
/// Tests for HtmlFormatter output, including XSS prevention.
/// </summary>
public class HtmlFormatterTests
{
    #region HTML Structure

    [Test]
    public async Task Format_IncludesDoctype()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).StartsWith("<!DOCTYPE html>");
    }

    [Test]
    public async Task Format_IncludesMetaCharset()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<meta charset=\"UTF-8\">");
    }

    [Test]
    public async Task Format_IncludesDefaultTitle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<title>Spec Results</title>");
    }

    [Test]
    public async Task Format_IncludesCustomTitle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter(new HtmlOptions { Title = "My Tests" });
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<title>My Tests</title>");
    }

    [Test]
    public async Task Format_IncludesStylesheet()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output)
            .Contains(
                "<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css\">");
    }

    [Test]
    public async Task Format_ProducesValidHtmlStructure()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<html lang=\"en\">");
        await Assert.That(output).Contains("<head>");
        await Assert.That(output).Contains("</head>");
        await Assert.That(output).Contains("<body>");
        await Assert.That(output).Contains("</body>");
        await Assert.That(output).Contains("</html>");
    }

    #endregion

    #region Content Escaping - XSS Prevention

    [Test]
    public async Task Format_EscapesSpecialCharsInDescription()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "<script>alert('xss')</script>", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        // Script tag should be escaped, not rendered
        await Assert.That(output).DoesNotContain("<script>");
        await Assert.That(output).Contains("&lt;script&gt;");
    }

    [Test]
    public async Task Format_EscapesSpecialCharsInError()
    {
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "failing spec",
                Status = "failed",
                Error = "<img src=x onerror=alert('xss')>"
            }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        // Error message should be escaped
        await Assert.That(output).DoesNotContain("<img src=x");
        await Assert.That(output).Contains("&lt;img");
    }

    [Test]
    public async Task Format_SanitizesCustomCss_RemovesStyleClose()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        // Attempt to break out of style tag
        var formatter = new HtmlFormatter(new HtmlOptions
        {
            CustomCss = ".custom { color: red; } </style><script>alert('xss')</script><style>"
        });
        var output = formatter.Format(report);

        // Should not contain the closing style tag that would break out
        await Assert.That(output).DoesNotContain("</style><script>");
    }

    [Test]
    public async Task Format_SanitizesCustomCss_RemovesScriptTags()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter(new HtmlOptions
        {
            CustomCss = ".custom { color: red; } <script>alert('xss')</script>"
        });
        var output = formatter.Format(report);

        // Should strip script tags from CSS
        await Assert.That(output).DoesNotContain("<script>");
    }

    [Test]
    public async Task Format_EscapesTitle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter(new HtmlOptions
        {
            Title = "<script>alert('xss')</script>"
        });
        var output = formatter.Format(report);

        // Title should be escaped
        await Assert.That(output).DoesNotContain("<title><script>");
        await Assert.That(output).Contains("&lt;script&gt;");
    }

    #endregion

    #region Status Rendering

    [Test]
    public async Task Format_PassedSpec_ShowsCheckmark()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "passing spec", Status = "passed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("✓");
        await Assert.That(output).Contains("class=\"passed\"");
    }

    [Test]
    public async Task Format_FailedSpec_ShowsX()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "failing spec", Status = "failed", Error = "assertion failed" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("✗");
        await Assert.That(output).Contains("class=\"failed\"");
        await Assert.That(output).Contains("class=\"error\"");
    }

    [Test]
    public async Task Format_PendingSpec_ShowsCircle()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pending spec", Status = "pending" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("○");
        await Assert.That(output).Contains("class=\"pending\"");
    }

    [Test]
    public async Task Format_SkippedSpec_ShowsDash()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "skipped spec", Status = "skipped" }
        ]);

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("−");
        await Assert.That(output).Contains("class=\"skipped\"");
    }

    #endregion

    #region Nesting and Structure

    [Test]
    public async Task Format_NestedContexts_UsesHeadingLevels()
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
                                    Specs = [new SpecResultReport { Description = "deep spec", Status = "passed" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<h1>Level 1</h1>");
        await Assert.That(output).Contains("<h2>Level 2</h2>");
        await Assert.That(output).Contains("<h3>Level 3</h3>");
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

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        // Level 6 and 7 should both use h6
        await Assert.That(output).Contains("<h6>L6</h6>");
        await Assert.That(output).Contains("<h6>Level 7</h6>");
    }

    [Test]
    public async Task Format_EmptyContext_NoSpecsList()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "Empty context",
                    Specs = [] // No specs
                }
            ]
        };

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        // Should have the heading but not a ul for empty specs
        await Assert.That(output).Contains("<h1>Empty context</h1>");
        // Count occurrences - should only have the built-in ul styling, not a spec list
        var ulCount = output.Split("<ul>").Length - 1;
        await Assert.That(ulCount).IsEqualTo(0);
    }

    [Test]
    public async Task Format_MultipleContexts_AllRendered()
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

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("First Context");
        await Assert.That(output).Contains("Second Context");
    }

    #endregion

    #region Summary and Metadata

    [Test]
    public async Task Format_IncludesSummaryStats()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 10, Passed = 5, Failed = 2, Pending = 2, Skipped = 1 },
            Contexts = []
        };

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("10 specs");
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

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("2025-12-15 10:30:45");
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

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("tests/my_spec.csx");
        await Assert.That(output).Contains("<code>");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task FileExtension_ReturnsHtml()
    {
        var formatter = new HtmlFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".html");
    }

    [Test]
    public async Task Format_EmptyReport_ProducesValidHtml()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts = []
        };

        var formatter = new HtmlFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).Contains("<!DOCTYPE html>");
        await Assert.That(output).Contains("0 specs");
        await Assert.That(output).Contains("</html>");
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