using DraftSpec.Plugins;

namespace DraftSpec.Configuration;

/// <summary>
/// Internal implementation of the reporter registry.
/// </summary>
internal class ReporterRegistry : IReporterRegistry
{
    private readonly List<IReporter> _reporters = [];
    private readonly Dictionary<string, IReporter> _byName = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        _reporters.Add(reporter);
        _byName[reporter.Name] = reporter;
    }

    public IEnumerable<IReporter> All => _reporters;

    public IReporter? Get(string name)
    {
        return _byName.TryGetValue(name, out var reporter) ? reporter : null;
    }
}
