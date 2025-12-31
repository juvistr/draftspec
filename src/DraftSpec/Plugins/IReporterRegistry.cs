namespace DraftSpec.Plugins;

/// <summary>
/// Registry for spec reporters.
/// </summary>
public interface IReporterRegistry
{
    /// <summary>
    /// Register a reporter.
    /// </summary>
    /// <param name="reporter">The reporter instance</param>
    void Register(IReporter reporter);

    /// <summary>
    /// Get all registered reporters.
    /// </summary>
    IEnumerable<IReporter> All { get; }

    /// <summary>
    /// Get a reporter by name.
    /// </summary>
    /// <param name="name">The reporter name</param>
    /// <returns>The reporter, or null if not found</returns>
    IReporter? GetByName(string name);
}