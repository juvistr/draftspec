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

    [Test]
    public async Task Generate_WithNewlineInDescription_EscapesNewline()
    {
        var input = new ScaffoldNode
        {
            Description = "line1\nline2",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("line1\\nline2");
        await Assert.That(output).DoesNotContain("line1\nline2");
    }

    [Test]
    public async Task Generate_WithCarriageReturnInDescription_EscapesCarriageReturn()
    {
        var input = new ScaffoldNode
        {
            Description = "line1\rline2",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("line1\\rline2");
    }

    [Test]
    public async Task Generate_WithTabInDescription_EscapesTab()
    {
        var input = new ScaffoldNode
        {
            Description = "col1\tcol2",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("col1\\tcol2");
    }

    [Test]
    public async Task Generate_WithBackslashInDescription_EscapesBackslash()
    {
        var input = new ScaffoldNode
        {
            Description = "path\\to\\file",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        await Assert.That(output).Contains("path\\\\to\\\\file");
    }

    #endregion

    #region Security - Code Injection Prevention

    [Test]
    public async Task Generate_WithInjectionAttempt_ProducesValidCode()
    {
        // Attempt to inject code via closing the string and adding code
        var input = new ScaffoldNode
        {
            Description = "test\"); maliciousCode(); describe(\"pwned",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        // The output should contain escaped quotes, keeping injection as string content
        await Assert.That(output).Contains("\\\""); // escaped quotes
        // Verify the structure: exactly one `=> {` pattern (single describe block body)
        var lambdaCount = output.Split("() =>").Length - 1;
        await Assert.That(lambdaCount).IsEqualTo(1);
        // Verify only one closing `});` pattern
        var closingCount = output.Split("});").Length - 1;
        await Assert.That(closingCount).IsEqualTo(1);
    }

    [Test]
    public async Task Generate_WithNewlineInjectionAttempt_ProducesValidCode()
    {
        // Attempt to inject code via newline
        var input = new ScaffoldNode
        {
            Description = "test\n\"); maliciousCode(); //",
            Specs = []
        };

        var output = Scaffolder.Generate(input);

        // Should escape the newline, keeping injection attempt as string content
        await Assert.That(output).Contains("\\n");
        await Assert.That(output).DoesNotContain("\n\");");
    }

    [Test]
    public async Task Generate_WithExcessiveNesting_ThrowsInvalidOperationException()
    {
        // Build a structure deeper than MaxDepth
        var root = new ScaffoldNode { Description = "level-0", Specs = [] };
        var current = root;

        for (var i = 1; i <= Scaffolder.MaxDepth + 1; i++)
        {
            var child = new ScaffoldNode { Description = $"level-{i}", Specs = [] };
            current.Contexts.Add(child);
            current = child;
        }

        await Assert.That(() => Scaffolder.Generate(root))
            .Throws<InvalidOperationException>()
            .WithMessageContaining($"{Scaffolder.MaxDepth}");
    }

    [Test]
    public async Task Generate_AtMaxDepth_Succeeds()
    {
        // Build a structure exactly at MaxDepth (should succeed)
        var root = new ScaffoldNode { Description = "level-0", Specs = [] };
        var current = root;

        // MaxDepth levels (0 to MaxDepth-1 = MaxDepth total levels)
        for (var i = 1; i < Scaffolder.MaxDepth; i++)
        {
            var child = new ScaffoldNode { Description = $"level-{i}", Specs = ["test"] };
            current.Contexts.Add(child);
            current = child;
        }

        // Should not throw
        var output = Scaffolder.Generate(root);

        await Assert.That(output).Contains($"level-{Scaffolder.MaxDepth - 1}");
    }

    #endregion
}
