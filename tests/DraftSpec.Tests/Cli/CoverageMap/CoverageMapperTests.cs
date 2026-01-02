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

    #region Method Name Matching

    [Test]
    public async Task MatchesSyncMethodToAsyncCall()
    {
        // Arrange - sync method, async call
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "Create", isAsync: false)
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
    }

    #endregion

    #region Type Reference Matching

    [Test]
    public async Task MediumConfidence_TypeOfReference()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("UserService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("checks type", typeRefs: [CreateTypeRef("UserService", ReferenceKind.TypeOf)])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Medium);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].MatchReason).Contains("typeof");
    }

    [Test]
    public async Task MediumConfidence_CastReference()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("UserService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("casts object", typeRefs: [CreateTypeRef("UserService", ReferenceKind.Cast)])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Medium);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].MatchReason).Contains("cast to");
    }

    [Test]
    public async Task MediumConfidence_VariableReference()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("UserService", "CreateAsync")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("declares variable", typeRefs: [CreateTypeRef("UserService", ReferenceKind.Variable)])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.Medium);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].MatchReason).Contains("variable of type");
    }

    #endregion

    #region Namespace Matching Edge Cases

    [Test]
    public async Task NoConfidence_EmptyMethodNamespace()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork", "") // Empty namespace
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("spec", usings: ["SomeNamespace"])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.None);
    }

    [Test]
    public async Task NoConfidence_EmptyUsingNamespace()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork", "MyNamespace")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("spec", usings: [""])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.None);
    }

    #endregion

    #region Display Name and Spec File

    [Test]
    public async Task BuildsDisplayNameWithContextPath()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork")
        };

        var specs = new List<SpecReference>
        {
            CreateSpecWithContext("does work", ["Parent", "Child"], methodCalls: [CreateMethodCall("DoWork")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].DisplayName).IsEqualTo("Parent > Child > does work");
    }

    [Test]
    public async Task BuildsDisplayNameWithEmptyContextPath()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork")
        };

        var specs = new List<SpecReference>
        {
            CreateSpecWithContext("root spec", [], methodCalls: [CreateMethodCall("DoWork")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].DisplayName).IsEqualTo("root spec");
    }

    [Test]
    public async Task ExtractsRelativeSpecFile()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("does work", methodCalls: [CreateMethodCall("DoWork")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].CoveringSpecs[0].SpecFile).IsEqualTo("test.spec.csx");
    }

    #endregion

    #region Sort Order

    [Test]
    public async Task SortsSpecsByConfidenceThenByName()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("TodoService", "CreateAsync", "MyApp.Services")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("z low confidence", usings: ["MyApp.Services"]),
            CreateSpec("a high confidence", methodCalls: [CreateMethodCall("CreateAsync")]),
            CreateSpec("m medium confidence", typeRefs: [CreateTypeRef("TodoService", ReferenceKind.New)])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        var coveringSpecs = result.AllMethods[0].CoveringSpecs;
        await Assert.That(coveringSpecs).Count().IsEqualTo(3);
        // Sorted by confidence (High > Medium > Low), then by name
        await Assert.That(coveringSpecs[0].Confidence).IsEqualTo(CoverageConfidence.High);
        await Assert.That(coveringSpecs[1].Confidence).IsEqualTo(CoverageConfidence.Medium);
        await Assert.That(coveringSpecs[2].Confidence).IsEqualTo(CoverageConfidence.Low);
    }

    [Test]
    public async Task SortsSpecsByNameWhenSameConfidence()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork")
        };

        var specs = new List<SpecReference>
        {
            CreateSpec("z spec", methodCalls: [CreateMethodCall("DoWork")]),
            CreateSpec("a spec", methodCalls: [CreateMethodCall("DoWork")]),
            CreateSpec("m spec", methodCalls: [CreateMethodCall("DoWork")])
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        var coveringSpecs = result.AllMethods[0].CoveringSpecs;
        await Assert.That(coveringSpecs).Count().IsEqualTo(3);
        // All HIGH confidence, sorted by name
        await Assert.That(coveringSpecs[0].DisplayName).Contains("a spec");
        await Assert.That(coveringSpecs[1].DisplayName).Contains("m spec");
        await Assert.That(coveringSpecs[2].DisplayName).Contains("z spec");
    }

    #endregion

    #region Paths in Result

    [Test]
    public async Task IncludesSourcePathInResult()
    {
        // Arrange
        var methods = new List<SourceMethod>();
        var specs = new List<SpecReference>();

        // Act
        var result = _mapper.Map(methods, specs, sourcePath: "src/Services/");

        // Assert
        await Assert.That(result.SourcePath).IsEqualTo("src/Services/");
    }

    [Test]
    public async Task IncludesSpecPathInResult()
    {
        // Arrange
        var methods = new List<SourceMethod>();
        var specs = new List<SpecReference>();

        // Act
        var result = _mapper.Map(methods, specs, specPath: "specs/unit/");

        // Assert
        await Assert.That(result.SpecPath).IsEqualTo("specs/unit/");
    }

    #endregion

    #region Empty Inputs

    [Test]
    public async Task HandlesEmptyMethodsList()
    {
        // Arrange
        var methods = new List<SourceMethod>();
        var specs = new List<SpecReference>
        {
            CreateSpec("some spec")
        };

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(0);
        await Assert.That(result.Summary.TotalMethods).IsEqualTo(0);
    }

    [Test]
    public async Task HandlesEmptySpecsList()
    {
        // Arrange
        var methods = new List<SourceMethod>
        {
            CreateMethod("Service", "DoWork")
        };
        var specs = new List<SpecReference>();

        // Act
        var result = _mapper.Map(methods, specs);

        // Assert
        await Assert.That(result.AllMethods).Count().IsEqualTo(1);
        await Assert.That(result.AllMethods[0].Confidence).IsEqualTo(CoverageConfidence.None);
        await Assert.That(result.AllMethods[0].CoveringSpecs).Count().IsEqualTo(0);
    }

    #endregion

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

    private static SpecReference CreateSpecWithContext(
        string description,
        IReadOnlyList<string> contextPath,
        IReadOnlyList<MethodCall>? methodCalls = null,
        IReadOnlyList<TypeReference>? typeRefs = null,
        IReadOnlyList<string>? usings = null)
    {
        var contextPathString = contextPath.Count > 0 ? string.Join("/", contextPath) + "/" : "";
        return new SpecReference
        {
            SpecId = $"test.spec.csx:{contextPathString}{description}",
            SpecDescription = description,
            ContextPath = contextPath.ToList(),
            MethodCalls = methodCalls ?? [],
            TypeReferences = typeRefs ?? [],
            UsingNamespaces = usings ?? [],
            SourceFile = "/test/test.spec.csx",
            LineNumber = 1
        };
    }
}
