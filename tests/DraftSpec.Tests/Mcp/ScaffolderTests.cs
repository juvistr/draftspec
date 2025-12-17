using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for the Scaffolder service that generates DraftSpec code from structured input.
/// </summary>
public class ScaffolderTests
{
    #region Simple Structure

    [Test]
    public async Task Generate_WithSpecs_GeneratesDescribeWithSpecs()
    {
        var input = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["adds numbers", "subtracts numbers"]
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("describe(\"Calculator\"");
        await Assert.That(output).Contains("it(\"adds numbers\")");
        await Assert.That(output).Contains("it(\"subtracts numbers\")");
    }

    [Test]
    public async Task Generate_WithSpecs_GeneratesPendingSpecs()
    {
        var input = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["adds numbers"]
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("it(\"adds numbers\");");
    }

    #endregion

    #region Nested Contexts

    [Test]
    public async Task Generate_WithNestedContext_GeneratesNestedDescribeBlocks()
    {
        var input = new ScaffoldNode
        {
            Description = "UserService",
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "Create",
                    Specs = ["creates user"]
                }
            ]
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("describe(\"UserService\"");
        await Assert.That(output).Contains("describe(\"Create\"");
        await Assert.That(output).Contains("it(\"creates user\")");
    }

    [Test]
    public async Task Generate_WithMultipleLevels_HandlesDeepNesting()
    {
        var input = new ScaffoldNode
        {
            Description = "UserService",
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "Create",
                    Contexts =
                    [
                        new ScaffoldNode
                        {
                            Description = "with valid input",
                            Specs = ["succeeds"]
                        }
                    ]
                }
            ]
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("describe(\"with valid input\"");
    }

    [Test]
    public async Task Generate_WithSpecsAndContexts_HandlesBothAtSameLevel()
    {
        var input = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["initializes to zero"],
            Contexts =
            [
                new ScaffoldNode { Description = "add", Specs = ["adds positive numbers"] }
            ]
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("it(\"initializes to zero\")");
        await Assert.That(output).Contains("describe(\"add\"");
    }

    #endregion

    #region Formatting

    [Test]
    public async Task Generate_WithNesting_IndentsCorrectly()
    {
        var input = new ScaffoldNode
        {
            Description = "Outer",
            Contexts = [new ScaffoldNode { Description = "Inner", Specs = ["works"] }]
        };

        var output = Scaffolder.Generate(input);
        var lines = output.Split('\n');

        var outerLine = lines.First(l => l.Contains("\"Outer\""));
        var innerLine = lines.First(l => l.Contains("\"Inner\""));

        var outerIndent = outerLine.TakeWhile(char.IsWhiteSpace).Count();
        var innerIndent = innerLine.TakeWhile(char.IsWhiteSpace).Count();

        await Assert.That(innerIndent).IsGreaterThan(outerIndent);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Generate_WithEmptySpecs_GeneratesEmptyDescribe()
    {
        var input = new ScaffoldNode
        {
            Description = "Empty",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("describe(\"Empty\"");
    }

    [Test]
    public async Task Generate_WithQuotesInDescription_EscapesQuotes()
    {
        var input = new ScaffoldNode
        {
            Description = "handles \"quoted\" strings",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("\\\"quoted\\\"");
        await Assert.That(output).DoesNotContain("\"\"quoted\"\"");
    }

    #endregion
}
