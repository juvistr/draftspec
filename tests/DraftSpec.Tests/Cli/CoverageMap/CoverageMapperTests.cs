using DraftSpec.Cli.CoverageMap;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class CoverageMapperTests
{
    private readonly CoverageMapper _mapper = new();

    [Test]
    public async Task HighConfidence_DirectMethodCall()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("creates todo", methodCalls: [CreateMethodCall("CreateAsync")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.High);
        await Assert.That(result.AllMethods[0].CoveringSpecs).Count().IsEqualTo(1);
    }

    [Test]
    public async Task MediumConfidence_TypeInstantiation()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("creates service", typeRefs: [CreateTypeRef("TodoService", ReferenceKind.New)])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Medium);
    }

    [Test]
    public async Task LowConfidence_NamespaceMatchOnly()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync", "MyApp.Services")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("does something", usings: ["MyApp.Services"])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Low);
    }

    [Test]
    public async Task NoConfidence_NoMatch()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync", "MyApp.Services")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("unrelated spec", usings: ["Other.Namespace"])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.None);
        await Assert.That(result.UncoveredMethods).Count().IsEqualTo(1);
    }

    [Test]
    public async Task PreferHighestConfidence()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync", "MyApp.Services")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("low confidence", usings: ["MyApp.Services"]),
            CreateSpec("high confidence", methodCalls: [CreateMethodCall("CreateAsync")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.High);
        await Assert.That(result.AllMethods[0].CoveringSpecs).Count().IsEqualTo(2);
        // Highest confidence should be first
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].Confidence).IsEqualTo(CoverageConfidence.High);
    }

    [Test]
    public async Task MatchesAsyncMethodVariants()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync", isAsync: true)
        };

        var specs = new List<SpecReference>
        {
            // Call without Async suffix should still match
            CreateSpec("creates todo", methodCalls: [CreateMethodCall("Create")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.High);
    }

    [Test]
    public async Task AggregatesMultipleSpecs()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("creates todo", methodCalls: [CreateMethodCall("CreateAsync")]),
            CreateSpec("validates input", methodCalls: [CreateMethodCall("CreateAsync")]),
            CreateSpec("handles errors", methodCalls: [CreateMethodCall("CreateAsync")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].CoveringSpecs).Count().IsEqualTo(3);
    }

    [Test]
    public async Task ComputesCorrectSummary()
    {
        // Arrange - use unique method names and separate class names to isolate confidence levels
        var methods = new List<SourceMethod>
        {
            CreateMethod("HighService", "DirectlyCalledMethod"),        // matched by direct call → High
            CreateMethod("MediumService", "TypeReferencedMethod"),      // matched by type ref → Medium
            CreateMethod("LowService", "NamespaceMatchedMethod", "MyApp"), // matched by using → Low
            CreateMethod("NoneService", "UnmatchedMethod")               // not matched → None
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("spec1", methodCalls: [CreateMethodCall("DirectlyCalledMethod")]),
            CreateSpec("spec2", typeRefs: [CreateTypeRef("MediumService", ReferenceKind.New)]),
            CreateSpec("spec3", usings: ["MyApp"])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.Summary.TotalMethods).IsEqualTo(4);
        await Assert.That(result.Summary.HighConfidence).IsEqualTo(1);
        await Assert.That(result.Summary.MediumConfidence).IsEqualTo(1);
        await Assert.That(result.Summary.LowConfidence).IsEqualTo(1);
        await Assert.That(result.Summary.Uncovered).IsEqualTo(1);
        await Assert.That(result.Summary.CoveragePercentage).IsEqualTo(75.0);
    }

    [Test]
    public async Task HandlesChildNamespaceMatching()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "Create", "MyApp.Services.Internal")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("spec", usings: ["MyApp.Services"])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Low);
    }

    [Test]
    public async Task IncludesMatchReasonInResult()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("creates todo", methodCalls: [CreateMethodCall("CreateAsync")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].MatchReason).Contains("Direct call");
    }

    // Helper methods
    private static SourceMethod CreateMethod(
        string className,
        string methodName,
        string? ns = null,
        bool isAsync = false)
    {
        return new SourceMethod
        {
            FullyQualifiedName = $"{ns ?? "Test"}.{className}.{methodName}",
            ClassName = className,
            MethodName = methodName,
            Signature = $"{methodName}()",
            Namespace = ns ?? "Test",
            SourceFile = "/test/file.cs",
            LineNumber = 1,
            IsAsync = isAsync
        };
    }

    private static SpecReference CreateSpec(
        string description,
        IReadOnlyList<MethodCall>? methodCalls = null,
        IReadOnlyList<TypeReference>? typeRefs = null,
        IReadOnlyList<string>? usings = null)
    {
        return new SpecReference
        {
            SpecId = $"test.spec.csx:Test/{description}",
            SpecDescription = description,
            ContextPath = ["Test"],
            MethodCalls = methodCalls ?? [],
            TypeReferences = typeRefs ?? [],
            UsingNamespaces = usings ?? [],
            SourceFile = "/test/test.spec.csx",
            LineNumber = 1
        };
    }

    private static MethodCall CreateMethodCall(string name)
    {
        return new MethodCall { MethodName = name, LineNumber = 1 };
    }

    private static TypeReference CreateTypeRef(string typeName, ReferenceKind kind)
    {
        return new TypeReference { TypeName = typeName, Kind = kind, LineNumber = 1 };
    }
}
