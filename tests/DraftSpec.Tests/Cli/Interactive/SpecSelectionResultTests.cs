using DraftSpec.Cli.Interactive;

namespace DraftSpec.Tests.Cli.Interactive;

/// <summary>
/// Tests for SpecSelectionResult factory methods.
/// </summary>
public class SpecSelectionResultTests
{
    #region Cancel

    [Test]
    public async Task Cancel_ReturnsCancelledResult()
    {
        var result = SpecSelectionResult.Cancel();

        await Assert.That(result.Cancelled).IsTrue();
    }

    [Test]
    public async Task Cancel_HasEmptySelectedSpecIds()
    {
        var result = SpecSelectionResult.Cancel();

        await Assert.That(result.SelectedSpecIds).IsEmpty();
    }

    [Test]
    public async Task Cancel_HasEmptySelectedDisplayNames()
    {
        var result = SpecSelectionResult.Cancel();

        await Assert.That(result.SelectedDisplayNames).IsEmpty();
    }

    [Test]
    public async Task Cancel_HasZeroTotalCount()
    {
        var result = SpecSelectionResult.Cancel();

        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    #endregion

    #region Success

    [Test]
    public async Task Success_SetsNotCancelled()
    {
        var result = SpecSelectionResult.Success(
            ["id1", "id2"],
            ["Display 1", "Display 2"],
            5);

        await Assert.That(result.Cancelled).IsFalse();
    }

    [Test]
    public async Task Success_SetsSelectedSpecIds()
    {
        var specIds = new List<string> { "spec1", "spec2", "spec3" };

        var result = SpecSelectionResult.Success(specIds, [], 10);

        await Assert.That(result.SelectedSpecIds).IsEquivalentTo(specIds);
    }

    [Test]
    public async Task Success_SetsSelectedDisplayNames()
    {
        var displayNames = new List<string> { "Context > spec1", "Context > spec2" };

        var result = SpecSelectionResult.Success([], displayNames, 10);

        await Assert.That(result.SelectedDisplayNames).IsEquivalentTo(displayNames);
    }

    [Test]
    public async Task Success_SetsTotalCount()
    {
        var result = SpecSelectionResult.Success(["id1"], ["name1"], 42);

        await Assert.That(result.TotalCount).IsEqualTo(42);
    }

    [Test]
    public async Task Success_WithEmptySelection_ReturnsValidResult()
    {
        var result = SpecSelectionResult.Success([], [], 10);

        await Assert.That(result.Cancelled).IsFalse();
        await Assert.That(result.SelectedSpecIds).IsEmpty();
        await Assert.That(result.SelectedDisplayNames).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(10);
    }

    [Test]
    public async Task Success_WithAllSelected_ReturnsFullSelection()
    {
        var ids = new List<string> { "a", "b", "c" };
        var names = new List<string> { "A", "B", "C" };

        var result = SpecSelectionResult.Success(ids, names, 3);

        await Assert.That(result.SelectedSpecIds).Count().IsEqualTo(3);
        await Assert.That(result.TotalCount).IsEqualTo(3);
    }

    #endregion

    #region Default Values

    [Test]
    public async Task DefaultInstance_HasEmptyCollections()
    {
        var result = new SpecSelectionResult();

        await Assert.That(result.SelectedSpecIds).IsEmpty();
        await Assert.That(result.SelectedDisplayNames).IsEmpty();
    }

    [Test]
    public async Task DefaultInstance_IsNotCancelled()
    {
        var result = new SpecSelectionResult();

        await Assert.That(result.Cancelled).IsFalse();
    }

    #endregion
}
