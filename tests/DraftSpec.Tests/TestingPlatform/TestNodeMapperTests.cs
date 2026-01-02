using DraftSpec.TestingPlatform;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace DraftSpec.Tests.TestingPlatform;

public class TestNodeMapperTests
{
    #region GenerateStableId Tests

    [Test]
    public async Task GenerateStableId_BuildsCorrectFormat()
    {
        // Arrange
        var relativeSourceFile = "Specs/math.spec.csx";
        var contextPath = new List<string> { "Calculator", "Add" };
        var specDescription = "adds two numbers";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/math.spec.csx:Calculator/Add/adds two numbers");
    }

    [Test]
    public async Task GenerateStableId_NormalizesBackslashesToForwardSlashes()
    {
        // Arrange
        var relativeSourceFile = @"Specs\nested\math.spec.csx";
        var contextPath = new List<string> { "Calculator" };
        var specDescription = "adds numbers";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/nested/math.spec.csx:Calculator/adds numbers");
    }

    [Test]
    public async Task GenerateStableId_EmptyContextPath_OmitsContextPart()
    {
        // Arrange
        var relativeSourceFile = "Specs/simple.spec.csx";
        var contextPath = new List<string>();
        var specDescription = "standalone spec";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/simple.spec.csx:/standalone spec");
    }

    [Test]
    public async Task GenerateStableId_SingleContextElement_IncludesContext()
    {
        // Arrange
        var relativeSourceFile = "Specs/basic.spec.csx";
        var contextPath = new List<string> { "Calculator" };
        var specDescription = "works";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/basic.spec.csx:Calculator/works");
    }

    [Test]
    public async Task GenerateStableId_DeeplyNestedContext_JoinsAllLevels()
    {
        // Arrange
        var relativeSourceFile = "Specs/deep.spec.csx";
        var contextPath = new List<string> { "Math", "Calculator", "Operations", "Add" };
        var specDescription = "handles decimals";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/deep.spec.csx:Math/Calculator/Operations/Add/handles decimals");
    }

    [Test]
    public async Task GenerateStableId_SpecDescriptionWithSpecialCharacters_PreservesCharacters()
    {
        // Arrange
        var relativeSourceFile = "Specs/special.spec.csx";
        var contextPath = new List<string> { "Parser" };
        var specDescription = "handles 'quotes' and \"double quotes\"";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/special.spec.csx:Parser/handles 'quotes' and \"double quotes\"");
    }

    [Test]
    public async Task GenerateStableId_MixedPathSeparators_NormalizesAll()
    {
        // Arrange - Windows-style mixed separators
        var relativeSourceFile = @"Specs\nested/deep\test.spec.csx";
        var contextPath = new List<string> { "Test" };
        var specDescription = "works";

        // Act
        var result = TestNodeMapper.GenerateStableId(relativeSourceFile, contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("Specs/nested/deep/test.spec.csx:Test/works");
    }

    #endregion

    #region GenerateDisplayName Tests

    [Test]
    public async Task GenerateDisplayName_ReturnsJustTheSpecDescription()
    {
        // Arrange
        var contextPath = new List<string> { "Calculator", "Add" };
        var specDescription = "adds two numbers";

        // Act
        var result = TestNodeMapper.GenerateDisplayName(contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("adds two numbers");
    }

    [Test]
    public async Task GenerateDisplayName_EmptyContextPath_ReturnsDescription()
    {
        // Arrange
        var contextPath = new List<string>();
        var specDescription = "standalone spec";

        // Act
        var result = TestNodeMapper.GenerateDisplayName(contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("standalone spec");
    }

    [Test]
    public async Task GenerateDisplayName_SingleContextElement_ReturnsDescription()
    {
        // Arrange
        var contextPath = new List<string> { "Calculator" };
        var specDescription = "works correctly";

        // Act
        var result = TestNodeMapper.GenerateDisplayName(contextPath, specDescription);

        // Assert
        await Assert.That(result).IsEqualTo("works correctly");
    }

    [Test]
    public async Task GenerateDisplayName_IgnoresContextPath()
    {
        // Arrange - context path should not affect display name
        var contextPath1 = new List<string> { "A", "B", "C" };
        var contextPath2 = new List<string> { "X", "Y", "Z" };
        var specDescription = "same description";

        // Act
        var result1 = TestNodeMapper.GenerateDisplayName(contextPath1, specDescription);
        var result2 = TestNodeMapper.GenerateDisplayName(contextPath2, specDescription);

        // Assert - both should return the same description regardless of context
        await Assert.That(result1).IsEqualTo(result2);
        await Assert.That(result1).IsEqualTo("same description");
    }

    #endregion

    #region CreateDiscoveryNode Tests

    [Test]
    public async Task CreateDiscoveryNode_ValidSpec_ReturnsCorrectUid()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "Specs/test.spec.csx:Calculator/adds numbers",
            Description = "adds numbers",
            DisplayName = "adds numbers",
            ContextPath = new[] { "Calculator" },
            SourceFile = "/project/Specs/test.spec.csx",
            RelativeSourceFile = "Specs/test.spec.csx",
            LineNumber = 10
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        await Assert.That(node.Uid.Value).IsEqualTo("Specs/test.spec.csx:Calculator/adds numbers");
        await Assert.That(node.DisplayName).IsEqualTo("adds numbers");
    }

    [Test]
    public async Task CreateDiscoveryNode_WithLineNumber_IncludesFileLocationProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 42
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var fileLocation = node.Properties.Single<TestFileLocationProperty>();
        await Assert.That(fileLocation).IsNotNull();
        await Assert.That(fileLocation.FilePath).IsEqualTo("/project/test.spec.csx");
        // Line 42 (1-based) becomes line 41 (0-based)
        await Assert.That(fileLocation.LineSpan.Start.Line).IsEqualTo(41);
    }

    [Test]
    public async Task CreateDiscoveryNode_WithoutLineNumber_OmitsFileLocationProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 0
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var hasFileLocation = node.Properties.Any<TestFileLocationProperty>();
        await Assert.That(hasFileLocation).IsFalse();
    }

    [Test]
    public async Task CreateDiscoveryNode_WithCompilationError_UsesFailedStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            CompilationError = "CS0103: The name 'undefined' does not exist"
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Explanation).Contains("Compilation error:");
        await Assert.That(failedState.Explanation).Contains("CS0103");
    }

    [Test]
    public async Task CreateDiscoveryNode_WithoutCompilationError_UsesDiscoveredStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var discoveredState = node.Properties.Single<DiscoveredTestNodeStateProperty>();
        await Assert.That(discoveredState).IsNotNull();
    }

    [Test]
    public async Task CreateDiscoveryNode_IncludesTestMethodIdentifier()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Calculator/adds numbers",
            Description = "adds numbers",
            DisplayName = "adds numbers",
            ContextPath = new[] { "Calculator" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var methodId = node.Properties.Single<TestMethodIdentifierProperty>();
        await Assert.That(methodId).IsNotNull();
        await Assert.That(methodId.TypeName).IsEqualTo("Calculator");
        await Assert.That(methodId.MethodName).IsEqualTo("adds numbers");
    }

    #endregion

    #region CreateResultNode (DiscoveredSpec) Tests

    [Test]
    public async Task CreateResultNode_PassedSpec_UsesPassedStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Passed,
            new[] { "Context" });

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var passedState = node.Properties.Single<PassedTestNodeStateProperty>();
        await Assert.That(passedState).IsNotNull();
    }

    [Test]
    public async Task CreateResultNode_FailedSpec_UsesFailedStatePropertyWithException()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var exception = new InvalidOperationException("Test error");
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Failed,
            new[] { "Context" },
            Exception: exception);

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Exception).IsEqualTo(exception);
        await Assert.That(failedState.Explanation).IsEqualTo("Test error");
    }

    [Test]
    public async Task CreateResultNode_FailedSpec_NoException_UsesGenericFailedMessage()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Failed,
            new[] { "Context" });

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Explanation).IsEqualTo("Test failed");
    }

    [Test]
    public async Task CreateResultNode_PendingSpec_UsesSkippedStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec"),
            SpecStatus.Pending,
            new[] { "Context" });

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var skippedState = node.Properties.Single<SkippedTestNodeStateProperty>();
        await Assert.That(skippedState).IsNotNull();
        await Assert.That(skippedState.Explanation).IsEqualTo("Pending - no implementation");
    }

    [Test]
    public async Task CreateResultNode_SkippedSpec_UsesSkippedStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Skipped,
            new[] { "Context" });

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var skippedState = node.Properties.Single<SkippedTestNodeStateProperty>();
        await Assert.That(skippedState).IsNotNull();
        await Assert.That(skippedState.Explanation).IsEqualTo("Skipped");
    }

    [Test]
    public async Task CreateResultNode_WithDuration_IncludesTimingProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Passed,
            new[] { "Context" },
            Duration: TimeSpan.FromSeconds(2));

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var timing = node.Properties.Single<TimingProperty>();
        await Assert.That(timing).IsNotNull();
        await Assert.That(timing.GlobalTiming.Duration).IsEqualTo(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task CreateResultNode_ZeroDuration_OmitsTimingProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Passed,
            new[] { "Context" },
            Duration: TimeSpan.Zero);

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var hasTiming = node.Properties.Any<TimingProperty>();
        await Assert.That(hasTiming).IsFalse();
    }

    #endregion

    #region CreateResultNode (path-based) Tests

    [Test]
    public async Task CreateResultNode_PathBased_GeneratesCorrectId()
    {
        // Arrange
        var result = new SpecResult(
            new SpecDefinition("adds numbers", () => { }),
            SpecStatus.Passed,
            new[] { "Calculator" });

        // Act
        var node = TestNodeMapper.CreateResultNode("Specs/test.spec.csx", "/project/Specs/test.spec.csx", result);

        // Assert
        await Assert.That(node.Uid.Value).IsEqualTo("Specs/test.spec.csx:Calculator/adds numbers");
    }

    [Test]
    public async Task CreateResultNode_PathBased_GeneratesDisplayName()
    {
        // Arrange
        var result = new SpecResult(
            new SpecDefinition("adds numbers", () => { }),
            SpecStatus.Passed,
            new[] { "Calculator" });

        // Act
        var node = TestNodeMapper.CreateResultNode("Specs/test.spec.csx", "/project/Specs/test.spec.csx", result);

        // Assert
        await Assert.That(node.DisplayName).IsEqualTo("adds numbers");
    }

    [Test]
    public async Task CreateResultNode_PathBased_IncludesFileLocation()
    {
        // Arrange
        var specDef = new SpecDefinition("adds numbers", () => { }) { LineNumber = 42 };
        var result = new SpecResult(
            specDef,
            SpecStatus.Passed,
            new[] { "Calculator" });

        // Act
        var node = TestNodeMapper.CreateResultNode("Specs/test.spec.csx", "/project/Specs/test.spec.csx", result);

        // Assert
        var fileLocation = node.Properties.Single<TestFileLocationProperty>();
        await Assert.That(fileLocation).IsNotNull();
        await Assert.That(fileLocation.FilePath).IsEqualTo("/project/Specs/test.spec.csx");
        await Assert.That(fileLocation.LineSpan.Start.Line).IsEqualTo(41); // 42 - 1
    }

    #endregion

    #region CreateCompilationErrorResultNode Tests

    [Test]
    public async Task CreateCompilationErrorResultNode_UsesFailedStateProperty()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            CompilationError = "CS0103: The name 'undefined' does not exist"
        };

        // Act
        var node = TestNodeMapper.CreateCompilationErrorResultNode(spec);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Explanation).Contains("Cannot execute:");
        await Assert.That(failedState.Explanation).Contains("CS0103");
    }

    [Test]
    public async Task CreateCompilationErrorResultNode_IncludesErrorMessage()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            CompilationError = "Missing semicolon"
        };

        // Act
        var node = TestNodeMapper.CreateCompilationErrorResultNode(spec);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState.Exception).IsNotNull();
        await Assert.That(failedState.Exception!.Message).IsEqualTo("Missing semicolon");
    }

    #endregion

    #region CreateErrorNode Tests

    [Test]
    public async Task CreateErrorNode_WithException_UsesExceptionInStateProperty()
    {
        // Arrange
        var exception = new InvalidOperationException("Compilation failed");
        var error = new DiscoveryError
        {
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            Message = "Failed to compile test.spec.csx",
            Exception = exception
        };

        // Act
        var node = TestNodeMapper.CreateErrorNode(error);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Exception).IsEqualTo(exception);
        await Assert.That(failedState.Explanation).IsEqualTo("Failed to compile test.spec.csx");
    }

    [Test]
    public async Task CreateErrorNode_WithoutException_CreatesNewException()
    {
        // Arrange
        var error = new DiscoveryError
        {
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            Message = "Failed to compile",
            Exception = null
        };

        // Act
        var node = TestNodeMapper.CreateErrorNode(error);

        // Assert
        var failedState = node.Properties.Single<FailedTestNodeStateProperty>();
        await Assert.That(failedState).IsNotNull();
        await Assert.That(failedState.Exception).IsNotNull();
        await Assert.That(failedState.Exception!.Message).IsEqualTo("Failed to compile");
    }

    [Test]
    public async Task CreateErrorNode_FileLocationPointsToLine1()
    {
        // Arrange
        var error = new DiscoveryError
        {
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            Message = "Error"
        };

        // Act
        var node = TestNodeMapper.CreateErrorNode(error);

        // Assert
        var fileLocation = node.Properties.Single<TestFileLocationProperty>();
        await Assert.That(fileLocation).IsNotNull();
        await Assert.That(fileLocation.FilePath).IsEqualTo("/project/test.spec.csx");
        await Assert.That(fileLocation.LineSpan.Start.Line).IsEqualTo(0); // Line 1 - 1 = 0
    }

    #endregion

    #region Metadata Property Tests

    [Test]
    public async Task CreateDiscoveryNode_WithTags_IncludesTestMetadataProperties()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            Tags = new[] { "slow", "integration" }
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var categories = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => p.Key == "Category")
            .Select(p => p.Value)
            .ToList();

        await Assert.That(categories).Contains("slow");
        await Assert.That(categories).Contains("integration");
    }

    [Test]
    public async Task CreateDiscoveryNode_NoTags_OmitsTagMetadata()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            Tags = Array.Empty<string>()
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var categories = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => p.Key == "Category")
            .ToList();

        await Assert.That(categories).IsEmpty();
    }

    [Test]
    public async Task CreateDiscoveryNode_FocusedSpec_IncludesStatusMetadata()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            IsFocused = true
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var status = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .FirstOrDefault(p => p.Key == "Status");

        await Assert.That(status).IsNotNull();
        await Assert.That(status!.Value).IsEqualTo("Focused");
    }

    [Test]
    public async Task CreateDiscoveryNode_SkippedSpec_IncludesStatusMetadata()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            IsSkipped = true
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var status = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .FirstOrDefault(p => p.Key == "Status");

        await Assert.That(status).IsNotNull();
        await Assert.That(status!.Value).IsEqualTo("Skipped");
    }

    [Test]
    public async Task CreateDiscoveryNode_PendingSpec_IncludesStatusMetadata()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            IsPending = true
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var status = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .FirstOrDefault(p => p.Key == "Status");

        await Assert.That(status).IsNotNull();
        await Assert.That(status!.Value).IsEqualTo("Pending");
    }

    [Test]
    public async Task CreateDiscoveryNode_NoSpecialStatus_OmitsStatusMetadata()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10
            // IsFocused, IsSkipped, IsPending all default to false
        };

        // Act
        var node = TestNodeMapper.CreateDiscoveryNode(spec);

        // Assert
        var statuses = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => p.Key == "Status")
            .ToList();

        await Assert.That(statuses).IsEmpty();
    }

    [Test]
    public async Task CreateResultNode_WithTags_IncludesTestMetadataProperties()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            Tags = new[] { "unit" }
        };
        var result = new SpecResult(
            new SpecDefinition("spec", () => { }),
            SpecStatus.Passed,
            new[] { "Context" });

        // Act
        var node = TestNodeMapper.CreateResultNode(spec, result);

        // Assert
        var categories = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => p.Key == "Category")
            .Select(p => p.Value)
            .ToList();

        await Assert.That(categories).Contains("unit");
    }

    [Test]
    public async Task CreateCompilationErrorResultNode_WithTags_IncludesTestMetadataProperties()
    {
        // Arrange
        var spec = new DiscoveredSpec
        {
            Id = "test.spec.csx:Context/spec",
            Description = "spec",
            DisplayName = "spec",
            ContextPath = new[] { "Context" },
            SourceFile = "/project/test.spec.csx",
            RelativeSourceFile = "test.spec.csx",
            LineNumber = 10,
            CompilationError = "CS0103: undefined",
            Tags = new[] { "slow" }
        };

        // Act
        var node = TestNodeMapper.CreateCompilationErrorResultNode(spec);

        // Assert
        var categories = node.Properties.AsEnumerable()
            .OfType<TestMetadataProperty>()
            .Where(p => p.Key == "Category")
            .Select(p => p.Value)
            .ToList();

        await Assert.That(categories).Contains("slow");
    }

    #endregion
}
