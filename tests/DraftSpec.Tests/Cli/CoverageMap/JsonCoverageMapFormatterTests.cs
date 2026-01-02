using System.Text.Json;
using DraftSpec.Cli.CoverageMap;
using DraftSpec.Cli.Formatters;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class JsonCoverageMapFormatterTests
{
    private readonly JsonCoverageMapFormatter _formatter = new();

    #region Valid JSON Output

    [Test]
    public async Task Format_ProducesValidJson()
    {
        var result = CreateResult();

        var output = _formatter.Format(result, gapsOnly: false);

        // Should not throw
        var doc = JsonDocument.Parse(output);
        await Assert.That(doc).IsNotNull();
    }

    [Test]
    public async Task Format_UsesIndentedFormatting()
    {
        var result = CreateResult();

        var output = _formatter.Format(result, gapsOnly: false);

        // Indented JSON has newlines
        await Assert.That(output).Contains("\n");
    }

    [Test]
    public async Task Format_UsesCamelCasePropertyNames()
    {
        var result = CreateResult();

        var output = _formatter.Format(result, gapsOnly: false);

        await Assert.That(output).Contains("\"totalMethods\"");
        await Assert.That(output).Contains("\"coveragePercentage\"");
        await Assert.That(output).Contains("\"byConfidence\"");
    }

    #endregion

    #region Summary Section

    [Test]
    public async Task Format_IncludesTotalMethods()
    {
        var result = CreateResultWithMethods(3);

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var totalMethods = json.RootElement.GetProperty("summary").GetProperty("totalMethods").GetInt32();
        await Assert.That(totalMethods).IsEqualTo(3);
    }

    [Test]
    public async Task Format_IncludesCoveragePercentage()
    {
        var result = CreateResultWithMethods(covered: 3, uncovered: 1);

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var percentage = json.RootElement.GetProperty("summary").GetProperty("coveragePercentage").GetDouble();
        await Assert.That(percentage).IsEqualTo(75.0);
    }

    [Test]
    public async Task Format_IncludesByConfidenceBreakdown()
    {
        var result = CreateResultWithConfidences(high: 2, medium: 1, low: 1, none: 1);

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var byConfidence = json.RootElement.GetProperty("summary").GetProperty("byConfidence");
        await Assert.That(byConfidence.GetProperty("high").GetInt32()).IsEqualTo(2);
        await Assert.That(byConfidence.GetProperty("medium").GetInt32()).IsEqualTo(1);
        await Assert.That(byConfidence.GetProperty("low").GetInt32()).IsEqualTo(1);
        await Assert.That(byConfidence.GetProperty("none").GetInt32()).IsEqualTo(1);
    }

    #endregion

    #region Paths and Settings

    [Test]
    public async Task Format_IncludesSourcePath()
    {
        var result = CreateResult(sourcePath: "src/Services/");

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var sourcePath = json.RootElement.GetProperty("sourcePath").GetString();
        await Assert.That(sourcePath).IsEqualTo("src/Services/");
    }

    [Test]
    public async Task Format_IncludesSpecPath()
    {
        var result = CreateResult(specPath: "specs/unit/");

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var specPath = json.RootElement.GetProperty("specPath").GetString();
        await Assert.That(specPath).IsEqualTo("specs/unit/");
    }

    [Test]
    public async Task Format_IncludesGapsOnlyFlag()
    {
        var result = CreateResult();

        var outputFalse = _formatter.Format(result, gapsOnly: false);
        var outputTrue = _formatter.Format(result, gapsOnly: true);

        var jsonFalse = JsonDocument.Parse(outputFalse);
        var jsonTrue = JsonDocument.Parse(outputTrue);

        await Assert.That(jsonFalse.RootElement.GetProperty("gapsOnly").GetBoolean()).IsFalse();
        await Assert.That(jsonTrue.RootElement.GetProperty("gapsOnly").GetBoolean()).IsTrue();
    }

    #endregion

    #region Methods Array

    [Test]
    public async Task Format_IncludesMethodDetails()
    {
        var result = CreateResultWithMethod(
            className: "UserService",
            methodName: "CreateAsync",
            signature: "CreateAsync(string)",
            ns: "MyApp.Services",
            lineNumber: 42
        );

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var method = json.RootElement.GetProperty("methods")[0];
        await Assert.That(method.GetProperty("className").GetString()).IsEqualTo("UserService");
        await Assert.That(method.GetProperty("methodName").GetString()).IsEqualTo("CreateAsync");
        await Assert.That(method.GetProperty("signature").GetString()).IsEqualTo("CreateAsync(string)");
        await Assert.That(method.GetProperty("namespace").GetString()).IsEqualTo("MyApp.Services");
        await Assert.That(method.GetProperty("lineNumber").GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task Format_IncludesFullyQualifiedName()
    {
        var result = CreateResultWithMethod(
            className: "UserService",
            methodName: "CreateAsync",
            ns: "MyApp.Services"
        );

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var fqn = json.RootElement.GetProperty("methods")[0].GetProperty("fullyQualifiedName").GetString();
        await Assert.That(fqn).IsEqualTo("MyApp.Services.UserService.CreateAsync");
    }

    [Test]
    public async Task Format_IncludesIsAsyncFlag()
    {
        var result = CreateResultWithMethod(className: "Service", methodName: "DoAsync", isAsync: true);

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var isAsync = json.RootElement.GetProperty("methods")[0].GetProperty("isAsync").GetBoolean();
        await Assert.That(isAsync).IsTrue();
    }

    [Test]
    public async Task Format_IncludesConfidenceAsLowerCase()
    {
        var result = CreateResultWithMethod(className: "Service", methodName: "Do", confidence: CoverageConfidence.High);

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var confidence = json.RootElement.GetProperty("methods")[0].GetProperty("confidence").GetString();
        await Assert.That(confidence).IsEqualTo("high");
    }

    #endregion

    #region Covering Specs

    [Test]
    public async Task Format_IncludesCoveringSpecs()
    {
        var result = CreateResultWithSpec(
            specId: "user.spec.csx:UserService/creates user",
            displayName: "creates user",
            matchReason: "Direct call to CreateAsync"
        );

        var output = _formatter.Format(result, gapsOnly: false);
        var json = JsonDocument.Parse(output);

        var specs = json.RootElement.GetProperty("methods")[0].GetProperty("coveringSpecs");
        await Assert.That(specs.GetArrayLength()).IsEqualTo(1);

        var spec = specs[0];
        await Assert.That(spec.GetProperty("specId").GetString()).IsEqualTo("user.spec.csx:UserService/creates user");
        await Assert.That(spec.GetProperty("displayName").GetString()).IsEqualTo("creates user");
        await Assert.That(spec.GetProperty("matchReason").GetString()).IsEqualTo("Direct call to CreateAsync");
    }

    #endregion

    #region GapsOnly Mode

    [Test]
    public async Task Format_GapsOnly_FiltersToUncoveredMethods()
    {
        var result = CreateResultWithConfidences(high: 2, none: 1);

        var output = _formatter.Format(result, gapsOnly: true);
        var json = JsonDocument.Parse(output);

        var methods = json.RootElement.GetProperty("methods");
        await Assert.That(methods.GetArrayLength()).IsEqualTo(1);
        await Assert.That(methods[0].GetProperty("confidence").GetString()).IsEqualTo("none");
    }

    #endregion

    // Helper methods
    private static CoverageMapResult CreateResult(string? sourcePath = "src/", string? specPath = "specs/")
    {
        return new CoverageMapResult
        {
            AllMethods = [],
            Summary = new CoverageSummary(),
            SourcePath = sourcePath,
            SpecPath = specPath
        };
    }

    private static CoverageMapResult CreateResultWithMethods(int count = 1, int covered = 0, int uncovered = 0)
    {
        var methods = new List<MethodCoverage>();
        int highCount = 0, noneCount = 0;

        if (covered + uncovered > 0)
        {
            for (int i = 0; i < covered; i++)
                methods.Add(CreateMethodCoverage($"Covered{i}", CoverageConfidence.High));
            for (int i = 0; i < uncovered; i++)
                methods.Add(CreateMethodCoverage($"Uncovered{i}", CoverageConfidence.None));
            highCount = covered;
            noneCount = uncovered;
        }
        else
        {
            for (int i = 0; i < count; i++)
                methods.Add(CreateMethodCoverage($"Method{i}", CoverageConfidence.High));
            highCount = count;
        }

        return new CoverageMapResult
        {
            AllMethods = methods,
            Summary = new CoverageSummary
            {
                TotalMethods = methods.Count,
                HighConfidence = highCount,
                Uncovered = noneCount
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static CoverageMapResult CreateResultWithConfidences(int high = 0, int medium = 0, int low = 0, int none = 0)
    {
        var methods = new List<MethodCoverage>();

        for (int i = 0; i < high; i++)
            methods.Add(CreateMethodCoverage($"High{i}", CoverageConfidence.High));
        for (int i = 0; i < medium; i++)
            methods.Add(CreateMethodCoverage($"Medium{i}", CoverageConfidence.Medium));
        for (int i = 0; i < low; i++)
            methods.Add(CreateMethodCoverage($"Low{i}", CoverageConfidence.Low));
        for (int i = 0; i < none; i++)
            methods.Add(CreateMethodCoverage($"None{i}", CoverageConfidence.None));

        return new CoverageMapResult
        {
            AllMethods = methods,
            Summary = new CoverageSummary
            {
                TotalMethods = high + medium + low + none,
                HighConfidence = high,
                MediumConfidence = medium,
                LowConfidence = low,
                Uncovered = none
            },
            SourcePath = "src/",
            SpecPath = "specs/"
        };
    }

    private static CoverageMapResult CreateResultWithMethod(
        string className = "Service",
        string methodName = "Method",
        string? signature = null,
        string ns = "Test",
        int lineNumber = 1,
        bool isAsync = false,
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
                        FullyQualifiedName = $"{ns}.{className}.{methodName}",
                        ClassName = className,
                        MethodName = methodName,
                        Signature = signature ?? $"{methodName}()",
                        Namespace = ns,
                        SourceFile = "/test/file.cs",
                        LineNumber = lineNumber,
                        IsAsync = isAsync
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

    private static CoverageMapResult CreateResultWithSpec(string specId, string displayName, string matchReason)
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
                            SpecId = specId,
                            DisplayName = displayName,
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

    private static MethodCoverage CreateMethodCoverage(string methodName, CoverageConfidence confidence)
    {
        return new MethodCoverage
        {
            Method = new SourceMethod
            {
                FullyQualifiedName = $"Test.Service.{methodName}",
                ClassName = "Service",
                MethodName = methodName,
                Signature = $"{methodName}()",
                Namespace = "Test",
                SourceFile = "/test/file.cs",
                LineNumber = 1
            },
            Confidence = confidence,
            CoveringSpecs = []
        };
    }
}
