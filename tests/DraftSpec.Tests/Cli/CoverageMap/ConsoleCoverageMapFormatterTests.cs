using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Formatters;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class ConsoleCoverageMapFormatterTests
{
    private readonly ConsoleCoverageMapFormatter _formatter = new();

    #region Header and Summary

    [Test]
    public async Task Format_IncludesCoveragePercentage()
    {
        var result = CreateResult(covered: 3, uncovered: 1);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("Coverage Map: 75.0%");
    }

    [Test]
    public async Task Format_IncludesMethodCounts()
    {
        var result = CreateResult(covered: 3, uncovered: 1);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("(3/4 methods)");
    }

    [Test]
    public async Task Format_IncludesConfidenceBadges()
    {
        var result = CreateResult(high: 1, medium: 1, low: 1, none: 1);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("[HIGH]");
        await Assert.That(output).Contains("[MEDIUM]");
        await Assert.That(output).Contains("[LOW]");
        await Assert.That(output).Contains("[NONE]");
    }

    #endregion

    #region Method Display

    [Test]
    public async Task Format_ShowsMethodSignature()
    {
        var result = CreateResultWithMethod("MyService", "DoWork", "DoWork(int, string)", 42);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("DoWork(int, string)");
    }

    [Test]
    public async Task Format_ShowsLineNumber()
    {
        var result = CreateResultWithMethod("MyService", "DoWork", "DoWork()", 42);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("(line 42)");
    }

    [Test]
    public async Task Format_GroupsMethodsByClass()
    {
        var result = CreateResultWithMethods(
            ("Service1", "MethodA"),
            ("Service2", "MethodB"),
            ("Service1", "MethodC")
        );

        var output = _formatter.Format(result, gapsOnly: false);

        // Verify classes are shown as headers
        await Assert.That(output).Contains("Test.Service1");
        await Assert.That(output).Contains("Test.Service2");
    }

    #endregion

    #region GapsOnly Mode

    [Test]
    public async Task Format_GapsOnly_ShowsOnlyUncoveredMethods()
    {
        var result = CreateResult(high: 1, none: 1);

        var output = _formatter.Format(result, gapsOnly: true);

        // Should show uncovered method details (NONE badge appears twice - in summary and in method list)
        await Assert.That(output).Contains("MethodNone0");
        // Should NOT show covered method details (MethodHigh0 should be filtered out)
        await Assert.That(output).DoesNotContain("MethodHigh0");
    }

    [Test]
    public async Task Format_GapsOnly_ShowsSuggestion()
    {
        var result = CreateResultWithMethod("MyService", "UncoveredMethod", "UncoveredMethod()", 10, CoverageConfidence.None);

        var output = _formatter.Format(result, gapsOnly: true);

        await Assert.That(output).Contains("Suggestion: Add spec for \"UncoveredMethod\"");
    }

    [Test]
    public async Task Format_GapsOnly_NoUncoveredMethods_ShowsMessage()
    {
        var result = CreateResult(high: 2, none: 0);

        var output = _formatter.Format(result, gapsOnly: true);

        await Assert.That(output).Contains("No uncovered methods found.");
    }

    #endregion

    #region Empty Results

    [Test]
    public async Task Format_NoMethods_ShowsMessage()
    {
        var result = CreateResult(high: 0, none: 0);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("No public methods found in source files.");
    }

    #endregion

    #region Covering Specs Display

    [Test]
    public async Task Format_ShowsCoveringSpecNames()
    {
        var result = CreateResultWithSpec("creates user", "Direct call to CreateUser");

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("creates user");
    }

    [Test]
    public async Task Format_ShowsMatchReason()
    {
        var result = CreateResultWithSpec("creates user", "Direct call to CreateUser");

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("Direct call to CreateUser");
    }

    [Test]
    public async Task Format_LimitsSpecsTo4()
    {
        var specs = Enumerable.Range(1, 6).Select(i => ($"spec {i}", $"Reason {i}")).ToArray();
        var result = CreateResultWithMultipleSpecs(specs);

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("spec 1");
        await Assert.That(output).Contains("spec 4");
        await Assert.That(output).Contains("... and 2 more");
    }

    #endregion

    // Helper methods
    private static CoverageMapResult CreateResult(int covered = 0, int uncovered = 0, int high = 0, int medium = 0, int low = 0, int none = 0)
    {
        var methods = new List<MethodCoverage>();
        int highCount = 0, mediumCount = 0, lowCount = 0, noneCount = 0;

        // If using high/medium/low/none params
        if (high + medium + low + none > 0)
        {
            methods.AddRange(CreateMethodsWithConfidence(high, CoverageConfidence.High));
            methods.AddRange(CreateMethodsWithConfidence(medium, CoverageConfidence.Medium));
            methods.AddRange(CreateMethodsWithConfidence(low, CoverageConfidence.Low));
            methods.AddRange(CreateMethodsWithConfidence(none, CoverageConfidence.None));
            highCount = high;
            mediumCount = medium;
            lowCount = low;
            noneCount = none;
        }
        // If using covered/uncovered params
        else
        {
            methods.AddRange(CreateMethodsWithConfidence(covered, CoverageConfidence.High));
            methods.AddRange(CreateMethodsWithConfidence(uncovered, CoverageConfidence.None));
            highCount = covered;
            noneCount = uncovered;
        }

        return new CoverageMapResult
        {
            AllMethods = methods,
            Summary = new CoverageSummary
            {
                TotalMethods = methods.Count,
                HighConfidence = highCount,
                MediumConfidence = mediumCount,
                LowConfidence = lowCount,
                Uncovered = noneCount
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static List<MethodCoverage> CreateMethodsWithConfidence(int count, CoverageConfidence confidence)
    {
        return Enumerable.Range(0, count).Select(i => new MethodCoverage
        {
            Method = new SourceMethod
            {
                FullyQualifiedName = $"Test.Service.Method{confidence}{i}",
                ClassName = "Service",
                MethodName = $"Method{confidence}{i}",
                Signature = $"Method{confidence}{i}()",
                Namespace = "Test",
                SourceFile = "/test/file.cs",
                LineNumber = i + 1
            },
            Confidence = confidence,
            CoveringSpecs = []
        }).ToList();
    }

    private static CoverageMapResult CreateResultWithMethod(
        string className,
        string methodName,
        string signature,
        int lineNumber,
        CoverageConfidence confidence = CoverageConfidence.High)
    {
        var isNone = confidence == CoverageConfidence.None;
        return new CoverageMapResult
        {
            AllMethods =
            [
                new MethodCoverage
                {
                    Method = new SourceMethod
                    {
                        FullyQualifiedName = $"Test.{className}.{methodName}",
                        ClassName = className,
                        MethodName = methodName,
                        Signature = signature,
                        Namespace = "Test",
                        SourceFile = "/test/file.cs",
                        LineNumber = lineNumber
                    },
                    Confidence = confidence,
                    CoveringSpecs = []
                }
            ],
            Summary = new CoverageSummary
            {
                TotalMethods = 1,
                HighConfidence = confidence == CoverageConfidence.High ? 1 : 0,
                MediumConfidence = confidence == CoverageConfidence.Medium ? 1 : 0,
                LowConfidence = confidence == CoverageConfidence.Low ? 1 : 0,
                Uncovered = isNone ? 1 : 0
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static CoverageMapResult CreateResultWithMethods(params (string className, string methodName)[] methods)
    {
        var methodList = methods.Select((m, i) => new MethodCoverage
        {
            Method = new SourceMethod
            {
                FullyQualifiedName = $"Test.{m.className}.{m.methodName}",
                ClassName = m.className,
                MethodName = m.methodName,
                Signature = $"{m.methodName}()",
                Namespace = "Test",
                SourceFile = "/test/file.cs",
                LineNumber = i + 1
            },
            Confidence = CoverageConfidence.High,
            CoveringSpecs = []
        }).ToList();

        return new CoverageMapResult
        {
            AllMethods = methodList,
            Summary = new CoverageSummary
            {
                TotalMethods = methodList.Count,
                HighConfidence = methodList.Count
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static CoverageMapResult CreateResultWithSpec(string specName, string matchReason)
    {
        return new CoverageMapResult
        {
            AllMethods =
            [
                new MethodCoverage
                {
                    Method = new SourceMethod
                    {
                        FullyQualifiedName = "Test.Service.Method",
                        ClassName = "Service",
                        MethodName = "Method",
                        Signature = "Method()",
                        Namespace = "Test",
                        SourceFile = "/test/file.cs",
                        LineNumber = 1
                    },
                    Confidence = CoverageConfidence.High,
                    CoveringSpecs =
                    [
                        new SpecCoverageInfo
                        {
                            SpecId = "test.spec.csx:Test/" + specName,
                            DisplayName = specName,
                            Confidence = CoverageConfidence.High,
                            MatchReason = matchReason,
                            SpecFile = "/test/test.spec.csx",
                            LineNumber = 1
                        }
                    ]
                }
            ],
            Summary = new CoverageSummary
            {
                TotalMethods = 1,
                HighConfidence = 1
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static CoverageMapResult CreateResultWithMultipleSpecs(params (string name, string reason)[] specs)
    {
        return new CoverageMapResult
        {
            AllMethods =
            [
                new MethodCoverage
                {
                    Method = new SourceMethod
                    {
                        FullyQualifiedName = "Test.Service.Method",
                        ClassName = "Service",
                        MethodName = "Method",
                        Signature = "Method()",
                        Namespace = "Test",
                        SourceFile = "/test/file.cs",
                        LineNumber = 1
                    },
                    Confidence = CoverageConfidence.High,
                    CoveringSpecs = specs.Select((s, i) => new SpecCoverageInfo
                    {
                        SpecId = $"test.spec.csx:Test/{s.name}",
                        DisplayName = s.name,
                        Confidence = CoverageConfidence.High,
                        MatchReason = s.reason,
                        SpecFile = "/test/test.spec.csx",
                        LineNumber = i + 1
                    }).ToList()
                }
            ],
            Summary = new CoverageSummary
            {
                TotalMethods = 1,
                HighConfidence = 1
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }
}
