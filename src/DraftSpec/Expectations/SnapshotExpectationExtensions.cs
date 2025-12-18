using System.Runtime.CompilerServices;
using DraftSpec.Snapshots;

namespace DraftSpec;

/// <summary>
/// Extension methods for snapshot testing on expectations.
/// </summary>
public static class SnapshotExpectationExtensions
{
    private static SnapshotManager? _manager;

    internal static SnapshotManager Manager
    {
        get => _manager ??= new SnapshotManager();
        set => _manager = value;
    }

    /// <summary>
    /// Assert that the actual value matches a stored snapshot.
    /// Creates a new snapshot on first run. Use DRAFTSPEC_UPDATE_SNAPSHOTS=true to update.
    /// </summary>
    /// <typeparam name="T">The type of value to snapshot</typeparam>
    /// <param name="expectation">The expectation containing the value to snapshot</param>
    /// <param name="snapshotName">Optional custom name for this snapshot</param>
    /// <param name="callerFilePath">Auto-captured path of the calling spec file</param>
    /// <exception cref="InvalidOperationException">When called outside an it() block</exception>
    /// <exception cref="AssertionException">When snapshot doesn't match</exception>
    public static void toMatchSnapshot<T>(
        this Expectation<T> expectation,
        string? snapshotName = null,
        [CallerFilePath] string callerFilePath = "")
    {
        var context = SnapshotContext.Current
            ?? throw new InvalidOperationException(
                "toMatchSnapshot() must be called inside an it() block. " +
                "Ensure you're calling this within a spec body.");

        var key = snapshotName ?? context.FullDescription;

        var result = Manager.Match(
            expectation.Actual,
            callerFilePath,
            key,
            snapshotName);

        switch (result.Status)
        {
            case SnapshotStatus.Matched:
            case SnapshotStatus.Created:
            case SnapshotStatus.Updated:
                return; // Test passes

            case SnapshotStatus.Missing:
                throw new AssertionException(
                    $"Snapshot \"{key}\" does not exist. " +
                    $"Run with {SnapshotManager.UpdateEnvVar}=true to create it.");

            case SnapshotStatus.Mismatched:
                throw new AssertionException(
                    $"Snapshot \"{key}\" does not match.\n\n{result.Diff}");
        }
    }
}
