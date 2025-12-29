using DraftSpec.Mcp.Models;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for SpecProgressNotification record.
/// </summary>
public class SpecProgressNotificationTests
{
    #region ProgressPercent Calculation

    [Test]
    public async Task ProgressPercent_WithPositiveTotal_CalculatesCorrectly()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 5,
            Total = 10
        };

        await Assert.That(notification.ProgressPercent).IsEqualTo(50.0);
    }

    [Test]
    public async Task ProgressPercent_WithZeroTotal_ReturnsZero()
    {
        var notification = new SpecProgressNotification
        {
            Type = "start",
            Completed = 0,
            Total = 0
        };

        await Assert.That(notification.ProgressPercent).IsEqualTo(0.0);
    }

    [Test]
    public async Task ProgressPercent_AllCompleted_Returns100()
    {
        var notification = new SpecProgressNotification
        {
            Type = "complete",
            Completed = 25,
            Total = 25
        };

        await Assert.That(notification.ProgressPercent).IsEqualTo(100.0);
    }

    [Test]
    public async Task ProgressPercent_PartialProgress_CalculatesFraction()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Completed = 1,
            Total = 3
        };

        // 1/3 = 33.333...%
        await Assert.That(notification.ProgressPercent).IsGreaterThan(33.3);
        await Assert.That(notification.ProgressPercent).IsLessThan(33.4);
    }

    #endregion

    #region Start Event

    [Test]
    public async Task StartEvent_HasCorrectProperties()
    {
        var notification = new SpecProgressNotification
        {
            Type = "start",
            Total = 42,
            Completed = 0
        };

        await Assert.That(notification.Type).IsEqualTo("start");
        await Assert.That(notification.Total).IsEqualTo(42);
        await Assert.That(notification.Completed).IsEqualTo(0);
        await Assert.That(notification.ProgressPercent).IsEqualTo(0.0);
    }

    #endregion

    #region Progress Event

    [Test]
    public async Task ProgressEvent_WithSpecAndStatus()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Spec = "UserService > creates a user",
            Status = "passed",
            Completed = 3,
            Total = 10,
            DurationMs = 45.5
        };

        await Assert.That(notification.Type).IsEqualTo("progress");
        await Assert.That(notification.Spec).IsEqualTo("UserService > creates a user");
        await Assert.That(notification.Status).IsEqualTo("passed");
        await Assert.That(notification.DurationMs).IsEqualTo(45.5);
        await Assert.That(notification.ProgressPercent).IsEqualTo(30.0);
    }

    [Test]
    public async Task ProgressEvent_FailedSpec()
    {
        var notification = new SpecProgressNotification
        {
            Type = "progress",
            Spec = "Calculator > divides by zero",
            Status = "failed",
            Completed = 5,
            Total = 20,
            DurationMs = 12.3
        };

        await Assert.That(notification.Status).IsEqualTo("failed");
        await Assert.That(notification.ProgressPercent).IsEqualTo(25.0);
    }

    #endregion

    #region Complete Event

    [Test]
    public async Task CompleteEvent_WithSummaryStats()
    {
        var notification = new SpecProgressNotification
        {
            Type = "complete",
            Completed = 100,
            Total = 100,
            Passed = 95,
            Failed = 2,
            Pending = 2,
            Skipped = 1,
            DurationMs = 5432.1
        };

        await Assert.That(notification.Type).IsEqualTo("complete");
        await Assert.That(notification.Passed).IsEqualTo(95);
        await Assert.That(notification.Failed).IsEqualTo(2);
        await Assert.That(notification.Pending).IsEqualTo(2);
        await Assert.That(notification.Skipped).IsEqualTo(1);
        await Assert.That(notification.DurationMs).IsEqualTo(5432.1);
        await Assert.That(notification.ProgressPercent).IsEqualTo(100.0);
    }

    [Test]
    public async Task CompleteEvent_AllPassed()
    {
        var notification = new SpecProgressNotification
        {
            Type = "complete",
            Completed = 50,
            Total = 50,
            Passed = 50,
            Failed = 0,
            Pending = 0,
            Skipped = 0,
            DurationMs = 1234.5
        };

        await Assert.That(notification.Passed).IsEqualTo(50);
        await Assert.That(notification.Failed).IsEqualTo(0);
    }

    #endregion

    #region Nullable Properties

    [Test]
    public async Task NullableProperties_DefaultToNull()
    {
        var notification = new SpecProgressNotification
        {
            Type = "start",
            Completed = 0,
            Total = 10
        };

        await Assert.That(notification.Spec).IsNull();
        await Assert.That(notification.Status).IsNull();
        await Assert.That(notification.Passed).IsNull();
        await Assert.That(notification.Failed).IsNull();
        await Assert.That(notification.Pending).IsNull();
        await Assert.That(notification.Skipped).IsNull();
    }

    #endregion
}
