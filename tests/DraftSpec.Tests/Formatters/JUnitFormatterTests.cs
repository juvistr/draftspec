using System.Xml.Linq;
using DraftSpec.Formatters;
using DraftSpec.Formatters.JUnit;

namespace DraftSpec.Tests.Formatters;

/// <summary>
/// Tests for JUnitFormatter XML output.
/// </summary>
public class JUnitFormatterTests
{
    #region XML Structure

    [Test]
    public async Task Format_ProducesValidXml()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);

        // Should not throw
        var doc = XDocument.Parse(output);
        await Assert.That(doc.Root).IsNotNull();
    }

    [Test]
    public async Task Format_HasXmlDeclaration()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);

        await Assert.That(output).StartsWith("<?xml version=\"1.0\"");
    }

    [Test]
    public async Task Format_RootElementIsTestsuites()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        await Assert.That(doc.Root!.Name.LocalName).IsEqualTo("testsuites");
    }

    [Test]
    public async Task Format_TestsuitesHasSummaryAttributes()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary
            {
                Total = 10,
                Passed = 5,
                Failed = 2,
                Pending = 2,
                Skipped = 1,
                DurationMs = 1234.5
            },
            Contexts = []
        };

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        await Assert.That(doc.Root!.Attribute("tests")!.Value).IsEqualTo("10");
        await Assert.That(doc.Root!.Attribute("failures")!.Value).IsEqualTo("2");
        await Assert.That(doc.Root!.Attribute("errors")!.Value).IsEqualTo("0");
        await Assert.That(doc.Root!.Attribute("skipped")!.Value).IsEqualTo("3"); // pending + skipped
        await Assert.That(doc.Root!.Attribute("time")!.Value).IsEqualTo("1.234");
    }

    #endregion

    #region Testsuite Elements

    [Test]
    public async Task Format_ContextBecomesTestsuite()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed" }
        ], "Calculator");

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testsuite = doc.Root!.Element("testsuite");
        await Assert.That(testsuite).IsNotNull();
        await Assert.That(testsuite!.Attribute("name")!.Value).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Format_TestsuiteHasStatsAttributes()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pass", Status = "passed", DurationMs = 100 },
            new SpecResultReport { Description = "fail", Status = "failed", DurationMs = 200 },
            new SpecResultReport { Description = "skip", Status = "skipped", DurationMs = 0 }
        ], "Suite");

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testsuite = doc.Root!.Element("testsuite");
        await Assert.That(testsuite!.Attribute("tests")!.Value).IsEqualTo("3");
        await Assert.That(testsuite!.Attribute("failures")!.Value).IsEqualTo("1");
        await Assert.That(testsuite!.Attribute("skipped")!.Value).IsEqualTo("1");
        await Assert.That(testsuite!.Attribute("time")!.Value).IsEqualTo("0.300");
    }

    [Test]
    public async Task Format_NestedContextsBecomeSeparateTestsuites()
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

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testsuites = doc.Root!.Elements("testsuite").ToList();
        await Assert.That(testsuites).Count().IsEqualTo(2);
        await Assert.That(testsuites[0].Attribute("name")!.Value).IsEqualTo("Parent");
        await Assert.That(testsuites[1].Attribute("name")!.Value).IsEqualTo("Parent.Child");
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
                    Description = "First",
                    Specs = [new SpecResultReport { Description = "spec1", Status = "passed" }]
                },
                new SpecContextReport
                {
                    Description = "Second",
                    Specs = [new SpecResultReport { Description = "spec2", Status = "passed" }]
                }
            ]
        };

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testsuites = doc.Root!.Elements("testsuite").ToList();
        await Assert.That(testsuites).Count().IsEqualTo(2);
        await Assert.That(testsuites[0].Attribute("name")!.Value).IsEqualTo("First");
        await Assert.That(testsuites[1].Attribute("name")!.Value).IsEqualTo("Second");
    }

    #endregion

    #region Testcase Elements

    [Test]
    public async Task Format_SpecBecomesTestcase()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "adds numbers", Status = "passed" }
        ], "Calculator");

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testcase = doc.Root!.Element("testsuite")!.Element("testcase");
        await Assert.That(testcase).IsNotNull();
        await Assert.That(testcase!.Attribute("name")!.Value).IsEqualTo("adds numbers");
        await Assert.That(testcase!.Attribute("classname")!.Value).IsEqualTo("Calculator");
    }

    [Test]
    public async Task Format_TestcaseHasTimeAttribute()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed", DurationMs = 123.456 }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testcase = doc.Root!.Element("testsuite")!.Element("testcase");
        await Assert.That(testcase!.Attribute("time")!.Value).IsEqualTo("0.123");
    }

    [Test]
    public async Task Format_PassedTestcase_NoChildElements()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "passes", Status = "passed" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testcase = doc.Root!.Element("testsuite")!.Element("testcase");
        await Assert.That(testcase!.HasElements).IsFalse();
    }

    #endregion

    #region Failed Testcases

    [Test]
    public async Task Format_FailedTestcase_HasFailureElement()
    {
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "fails",
                Status = "failed",
                Error = "Expected 5 but was 4"
            }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var failure = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("failure");
        await Assert.That(failure).IsNotNull();
    }

    [Test]
    public async Task Format_FailureElement_HasMessageAttribute()
    {
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "fails",
                Status = "failed",
                Error = "First line of error\nSecond line\nThird line"
            }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var failure = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("failure");
        await Assert.That(failure!.Attribute("message")!.Value).IsEqualTo("First line of error");
    }

    [Test]
    public async Task Format_FailureElement_HasTypeAttribute()
    {
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "fails",
                Status = "failed",
                Error = "error message"
            }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var failure = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("failure");
        await Assert.That(failure!.Attribute("type")!.Value).IsEqualTo("AssertionError");
    }

    [Test]
    public async Task Format_FailureElement_ContainsFullError()
    {
        var fullError = "Expected 5 but was 4\n   at Test.Method() line 42";
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "fails",
                Status = "failed",
                Error = fullError
            }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var failure = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("failure");
        await Assert.That(failure!.Value).IsEqualTo(fullError);
    }

    [Test]
    public async Task Format_FailedTestcase_NoError_DefaultMessage()
    {
        var report = CreateReport([
            new SpecResultReport
            {
                Description = "fails",
                Status = "failed",
                Error = null
            }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var failure = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("failure");
        await Assert.That(failure!.Attribute("message")!.Value).IsEqualTo("Assertion failed");
    }

    #endregion

    #region Skipped and Pending Testcases

    [Test]
    public async Task Format_SkippedTestcase_HasSkippedElement()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "skipped", Status = "skipped" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var skipped = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("skipped");
        await Assert.That(skipped).IsNotNull();
    }

    [Test]
    public async Task Format_PendingTestcase_HasSkippedElement()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pending", Status = "pending" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var skipped = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("skipped");
        await Assert.That(skipped).IsNotNull();
    }

    [Test]
    public async Task Format_PendingTestcase_HasMessageAttribute()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "pending", Status = "pending" }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var skipped = doc.Root!.Element("testsuite")!.Element("testcase")!.Element("skipped");
        await Assert.That(skipped!.Attribute("message")!.Value).Contains("not yet implemented");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task FileExtension_ReturnsXml()
    {
        var formatter = new JUnitFormatter();

        await Assert.That(formatter.FileExtension).IsEqualTo(".xml");
    }

    [Test]
    public async Task Format_EmptyReport_ProducesValidXml()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 0 },
            Contexts = []
        };

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        await Assert.That(doc.Root!.Name.LocalName).IsEqualTo("testsuites");
        await Assert.That(doc.Root!.Elements("testsuite")).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Format_SpecialCharactersInDescription_AreEscaped()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "handles <xml> & \"quotes\"", Status = "passed" }
        ], "Test & <Context>");

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);

        // Should be valid XML (would throw if not properly escaped)
        var doc = XDocument.Parse(output);

        var testsuite = doc.Root!.Element("testsuite");
        await Assert.That(testsuite!.Attribute("name")!.Value).IsEqualTo("Test & <Context>");

        var testcase = testsuite!.Element("testcase");
        await Assert.That(testcase!.Attribute("name")!.Value).IsEqualTo("handles <xml> & \"quotes\"");
    }

    [Test]
    public async Task Format_ZeroDuration_FormatsCorrectly()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "fast", Status = "passed", DurationMs = 0 }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testcase = doc.Root!.Element("testsuite")!.Element("testcase");
        await Assert.That(testcase!.Attribute("time")!.Value).IsEqualTo("0.000");
    }

    [Test]
    public async Task Format_NullDuration_DefaultsToZero()
    {
        var report = CreateReport([
            new SpecResultReport { Description = "spec", Status = "passed", DurationMs = null }
        ]);

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testcase = doc.Root!.Element("testsuite")!.Element("testcase");
        await Assert.That(testcase!.Attribute("time")!.Value).IsEqualTo("0.000");
    }

    [Test]
    public async Task Format_DeepNesting_UsesFullPath()
    {
        var report = new SpecReport
        {
            Timestamp = DateTime.UtcNow,
            Summary = new SpecSummary { Total = 1, Passed = 1 },
            Contexts =
            [
                new SpecContextReport
                {
                    Description = "A",
                    Contexts =
                    [
                        new SpecContextReport
                        {
                            Description = "B",
                            Contexts =
                            [
                                new SpecContextReport
                                {
                                    Description = "C",
                                    Specs = [new SpecResultReport { Description = "spec", Status = "passed" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var formatter = new JUnitFormatter();
        var output = formatter.Format(report);
        var doc = XDocument.Parse(output);

        var testsuites = doc.Root!.Elements("testsuite").ToList();
        await Assert.That(testsuites.Last().Attribute("name")!.Value).IsEqualTo("A.B.C");
    }

    #endregion

    #region CLI Integration

    [Test]
    public async Task CliFormatterRegistry_ReturnsJUnitFormatter()
    {
        var registry = new DraftSpec.Cli.DependencyInjection.CliFormatterRegistry();

        var formatter = registry.GetFormatter("junit");

        await Assert.That(formatter).IsNotNull();
        await Assert.That(formatter).IsTypeOf<JUnitFormatter>();
    }

    [Test]
    public async Task OutputFormats_HasJUnitConstant()
    {
        await Assert.That(DraftSpec.Cli.OutputFormats.JUnit).IsEqualTo("junit");
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
