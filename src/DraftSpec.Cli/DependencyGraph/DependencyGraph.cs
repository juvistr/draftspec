namespace DraftSpec.Cli.DependencyGraph;

/// <summary>
/// Represents the dependency graph between spec files and source files.
/// Supports both forward lookups (spec → dependencies) and reverse lookups (source → affected specs).
/// </summary>
public class DependencyGraph
{
    private readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// Maps spec file → all files it depends on (via #load, directly or transitively).
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _specDependencies;

    /// <summary>
    /// Maps source file → all spec files that depend on it (reverse index for impact analysis).
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _reverseDependencies;

    /// <summary>
    /// Maps namespace → source files that define types in that namespace.
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _namespaceToFiles;

    /// <summary>
    /// Maps spec file → namespaces it uses (via using directives).
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _specNamespaces;

    public DependencyGraph()
    {
        _specDependencies = new Dictionary<string, HashSet<string>>(_pathComparer);
        _reverseDependencies = new Dictionary<string, HashSet<string>>(_pathComparer);
        _namespaceToFiles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        _specNamespaces = new Dictionary<string, HashSet<string>>(_pathComparer);
    }

    /// <summary>
    /// All registered spec files.
    /// </summary>
    public IReadOnlyCollection<string> SpecFiles => _specDependencies.Keys;

    /// <summary>
    /// All registered source files (from namespace mapping).
    /// </summary>
    public IReadOnlyCollection<string> SourceFiles => _namespaceToFiles.Values
        .SelectMany(f => f)
        .Distinct(_pathComparer)
        .ToList();

    /// <summary>
    /// Adds a spec and its dependencies to the graph.
    /// </summary>
    public void AddSpec(SpecDependency dependency)
    {
        var specFile = dependency.SpecFile;

        // Initialize or get dependency set for this spec
        if (!_specDependencies.TryGetValue(specFile, out var deps))
        {
            deps = new HashSet<string>(_pathComparer);
            _specDependencies[specFile] = deps;
        }

        // Add all #load dependencies
        foreach (var loadDep in dependency.LoadDependencies)
        {
            deps.Add(loadDep);

            // Add reverse mapping
            if (!_reverseDependencies.TryGetValue(loadDep, out var specs))
            {
                specs = new HashSet<string>(_pathComparer);
                _reverseDependencies[loadDep] = specs;
            }
            specs.Add(specFile);
        }

        // Track namespaces used by this spec
        if (!_specNamespaces.TryGetValue(specFile, out var namespaces))
        {
            namespaces = new HashSet<string>(StringComparer.Ordinal);
            _specNamespaces[specFile] = namespaces;
        }

        foreach (var ns in dependency.Namespaces)
        {
            namespaces.Add(ns);
        }
    }

    /// <summary>
    /// Registers a source file as defining types in a namespace.
    /// </summary>
    public void AddNamespaceMapping(string sourceFile, string @namespace)
    {
        if (!_namespaceToFiles.TryGetValue(@namespace, out var files))
        {
            files = new HashSet<string>(_pathComparer);
            _namespaceToFiles[@namespace] = files;
        }
        files.Add(sourceFile);
    }

    /// <summary>
    /// Gets spec files that are directly or indirectly affected by changes to the specified files.
    /// </summary>
    public IReadOnlySet<string> GetAffectedSpecs(IEnumerable<string> changedFiles)
    {
        var affected = new HashSet<string>(_pathComparer);

        foreach (var changedFile in changedFiles)
        {
            var normalizedPath = Path.GetFullPath(changedFile);

            // 1. If the changed file is a spec file itself, include it
            if (_specDependencies.ContainsKey(normalizedPath))
            {
                affected.Add(normalizedPath);
            }

            // 2. Check direct reverse dependencies (files loaded via #load)
            if (_reverseDependencies.TryGetValue(normalizedPath, out var dependentSpecs))
            {
                foreach (var spec in dependentSpecs)
                {
                    affected.Add(spec);
                }
            }

            // 3. Check namespace-based dependencies for .cs files
            if (normalizedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                // Find which namespaces this file defines
                foreach (var (ns, files) in _namespaceToFiles)
                {
                    if (files.Contains(normalizedPath))
                    {
                        // Find all specs that use this namespace
                        foreach (var (specFile, specNamespaces) in _specNamespaces)
                        {
                            if (specNamespaces.Contains(ns))
                            {
                                affected.Add(specFile);
                            }
                        }
                    }
                }
            }
        }

        return affected;
    }

    /// <summary>
    /// Gets all files that a spec depends on (via #load).
    /// </summary>
    public IReadOnlySet<string> GetDependencies(string specFile)
    {
        if (_specDependencies.TryGetValue(specFile, out var deps))
        {
            return deps;
        }
        return new HashSet<string>(_pathComparer);
    }

    /// <summary>
    /// Gets namespaces used by a spec (via using directives).
    /// </summary>
    public IReadOnlySet<string> GetNamespaces(string specFile)
    {
        if (_specNamespaces.TryGetValue(specFile, out var namespaces))
        {
            return namespaces;
        }
        return new HashSet<string>(StringComparer.Ordinal);
    }
}
