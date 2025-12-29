using DraftSpec.TestingPlatform;

namespace DraftSpec.Tests.TestingPlatform;

public class TestNodeMapperTests
{
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
}
