namespace DraftSpec.Tests.Infrastructure;

/// <summary>
/// Tracks execution order of events in tests.
/// Useful for verifying hook ordering and execution sequences.
/// </summary>
/// <example>
/// <code>
/// var tracker = new ExecutionTracker();
/// context.BeforeEach = tracker.CreateTracker("beforeEach");
/// context.AfterEach = tracker.CreateTracker("afterEach");
/// context.AddSpec(new SpecDefinition("spec", tracker.CreateTracker("spec")));
///
/// await runner.RunAsync(context);
///
/// await Assert.That(tracker.Events).IsEquivalentTo(["beforeEach", "spec", "afterEach"]);
/// </code>
/// </example>
public class ExecutionTracker
{
    private readonly List<string> _events = [];

    /// <summary>
    /// Gets the recorded events in order of execution.
    /// </summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>
    /// Records an event with the specified name.
    /// </summary>
    /// <param name="eventName">The name of the event to record.</param>
    public void Track(string eventName) => _events.Add(eventName);

    /// <summary>
    /// Creates a synchronous action that records the specified event when invoked.
    /// </summary>
    /// <param name="eventName">The name of the event to record.</param>
    /// <returns>An action that records the event.</returns>
    public Action CreateTracker(string eventName) => () => Track(eventName);

    /// <summary>
    /// Creates an asynchronous function that records the specified event when invoked.
    /// </summary>
    /// <param name="eventName">The name of the event to record.</param>
    /// <returns>A function that records the event and returns a completed task.</returns>
    public Func<Task> CreateAsyncTracker(string eventName) => () =>
    {
        Track(eventName);
        return Task.CompletedTask;
    };

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    public void Clear() => _events.Clear();
}
