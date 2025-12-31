using DraftSpec.Mcp.Models;
using DraftSpec.Mcp.Tools;

namespace DraftSpec.Tests.Mcp.Tools;

/// <summary>
/// Tests for SpecTools helper methods.
/// Note: Full RunSpec/RunSpecsBatch integration tests require subprocess execution
/// and are covered by SpecRunOrchestratorTests (with mocks) and CLI integration tests.
/// </summary>
public class SpecToolsTests
{
    #region scaffold_specs

    [Test]
    public async Task ScaffoldSpecs_SimpleStructure_GeneratesCode()
    {
        var structure = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["add", "subtract"]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Calculator\"");
        await Assert.That(result).Contains("it(\"add\"");
        await Assert.That(result).Contains("it(\"subtract\"");
    }

    [Test]
    public async Task ScaffoldSpecs_NestedStructure_GeneratesNestedCode()
    {
        var structure = new ScaffoldNode
        {
            Description = "Math",
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "basic operations",
                    Specs = ["adds numbers", "subtracts numbers"]
                }
            ]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Math\"");
        await Assert.That(result).Contains("describe(\"basic operations\"");
        await Assert.That(result).Contains("it(\"adds numbers\"");
    }

    [Test]
    public async Task ScaffoldSpecs_EmptyStructure_GeneratesEmptyDescribe()
    {
        var structure = new ScaffoldNode
        {
            Description = "Empty"
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Empty\"");
    }

    [Test]
    public async Task ScaffoldSpecs_WithSpecsAndContexts_GeneratesBoth()
    {
        var structure = new ScaffoldNode
        {
            Description = "Calculator",
            Specs = ["should exist"],
            Contexts =
            [
                new ScaffoldNode
                {
                    Description = "addition",
                    Specs = ["adds positive numbers", "adds negative numbers"]
                }
            ]
        };

        var result = SpecTools.ScaffoldSpecs(structure);

        await Assert.That(result).Contains("describe(\"Calculator\"");
        await Assert.That(result).Contains("it(\"should exist\"");
        await Assert.That(result).Contains("describe(\"addition\"");
        await Assert.That(result).Contains("it(\"adds positive numbers\"");
    }

    #endregion

    #region FormatProgressMessage Tests

    [Test]
    public async Task FormatProgressMessage_StartType_ReturnsStartingMessage()
    {
        var notification = new SpecProgressNotification { Type = "start", Total = 5 };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("Starting 5 specs...");
    }

    [Test]
    public async Task FormatProgressMessage_ProgressType_ReturnsProgressMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 3,
            Total = 10,
            Status = "passed",
            Spec = "adds numbers"
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("[3/10] passed: adds numbers");
    }

    [Test]
    public async Task FormatProgressMessage_CompleteType_ReturnsCompletedMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "complete",
            Passed = 8,
            Failed = 2
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("Completed: 8 passed, 2 failed");
    }

    [Test]
    public async Task FormatProgressMessage_UnknownType_ReturnsTypeName()
    {
        var notification = new SpecProgressNotification { Type = "custom" };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("custom");
    }

    [Test]
    public async Task FormatProgressMessage_ProgressWithFailed_ReturnsFormattedMessage()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 5,
            Total = 10,
            Status = "failed",
            Spec = "throws exception"
        };

        var result = SpecTools.FormatProgressMessage(notification);

        await Assert.That(result).IsEqualTo("[5/10] failed: throws exception");
    }

    #endregion

    #region FormatBatchProgressMessage Tests

    [Test]
    public async Task FormatBatchProgressMessage_ReturnsFormattedMessage()
    {
        var result = SpecTools.FormatBatchProgressMessage(3, 10, "Calculator");

        await Assert.That(result).IsEqualTo("[3/10] Completed: Calculator");
    }

    [Test]
    public async Task FormatBatchProgressMessage_FirstSpec_ShowsCorrectProgress()
    {
        var result = SpecTools.FormatBatchProgressMessage(1, 5, "FirstSpec");

        await Assert.That(result).IsEqualTo("[1/5] Completed: FirstSpec");
    }

    [Test]
    public async Task FormatBatchProgressMessage_LastSpec_ShowsCorrectProgress()
    {
        var result = SpecTools.FormatBatchProgressMessage(5, 5, "LastSpec");

        await Assert.That(result).IsEqualTo("[5/5] Completed: LastSpec");
    }

    #endregion
}
