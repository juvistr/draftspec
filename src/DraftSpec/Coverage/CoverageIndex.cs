using System.Collections.Concurrent;

namespace DraftSpec.Coverage;

/// <summary>
/// Reverse index mapping source code locations to specs that cover them.
/// Enables "which specs cover this line?" queries for test impact analysis.
/// </summary>
/// <remarks>
/// Thread-safe for use during parallel spec execution.
/// </remarks>
public class CoverageIndex
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ConcurrentBag<string>>> _index = new();

    /// <summary>
    /// Add coverage data from a spec execution to the index.
    /// </summary>
    /// <param name="data">Coverage data from a spec</param>
    public void AddSpecCoverage(SpecCoverageData data)
    {
        foreach (var (filePath, file) in data.FilesCovered)
        {
            var fileIndex = _index.GetOrAdd(filePath, _ => new ConcurrentDictionary<int, ConcurrentBag<string>>());

            foreach (var lineNumber in file.LineHits.Keys)
            {
                var specs = fileIndex.GetOrAdd(lineNumber, _ => new ConcurrentBag<string>());
                specs.Add(data.SpecId);
            }
        }
    }

    /// <summary>
    /// Get all specs that cover a specific line in a file.
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <param name="lineNumber">Line number (1-based)</param>
    /// <returns>List of spec IDs that cover this line</returns>
    public IReadOnlyList<string> GetSpecsForLine(string filePath, int lineNumber)
    {
        if (!_index.TryGetValue(filePath, out var fileIndex))
            return [];

        if (!fileIndex.TryGetValue(lineNumber, out var specs))
            return [];

        return specs.ToArray();
    }

    /// <summary>
    /// Get all specs that cover any line in a file.
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <returns>Set of unique spec IDs that cover any line in this file</returns>
    public IReadOnlySet<string> GetSpecsForFile(string filePath)
    {
        if (!_index.TryGetValue(filePath, out var fileIndex))
            return new HashSet<string>();

        var specs = new HashSet<string>();
        foreach (var lineSpecs in fileIndex.Values)
        {
            foreach (var specId in lineSpecs)
            {
                specs.Add(specId);
            }
        }

        return specs;
    }

    /// <summary>
    /// Get all specs that cover any of the specified lines in a file.
    /// Useful for determining which specs to re-run when specific lines change.
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <param name="lineNumbers">Line numbers to check</param>
    /// <returns>Set of unique spec IDs that cover any of the specified lines</returns>
    public IReadOnlySet<string> GetSpecsForLines(string filePath, IEnumerable<int> lineNumbers)
    {
        if (!_index.TryGetValue(filePath, out var fileIndex))
            return new HashSet<string>();

        var specs = new HashSet<string>();
        foreach (var lineNumber in lineNumbers)
        {
            if (fileIndex.TryGetValue(lineNumber, out var lineSpecs))
            {
                foreach (var specId in lineSpecs)
                {
                    specs.Add(specId);
                }
            }
        }

        return specs;
    }

    /// <summary>
    /// Get coverage statistics for a file.
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <returns>Dictionary of line number to number of specs covering that line</returns>
    public IReadOnlyDictionary<int, int> GetLineCoverageCounts(string filePath)
    {
        if (!_index.TryGetValue(filePath, out var fileIndex))
            return new Dictionary<int, int>();

        return fileIndex.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count);
    }

    /// <summary>
    /// Get all files that have any coverage data.
    /// </summary>
    /// <returns>List of file paths with coverage</returns>
    public IReadOnlyList<string> GetCoveredFiles()
    {
        return _index.Keys.ToArray();
    }

    /// <summary>
    /// Get the total number of unique lines covered across all files.
    /// </summary>
    public int TotalLinesCovered => _index.Values.Sum(f => f.Count);

    /// <summary>
    /// Get the total number of files with coverage data.
    /// </summary>
    public int TotalFilesCovered => _index.Count;

    /// <summary>
    /// Clear all coverage index data.
    /// </summary>
    public void Clear()
    {
        _index.Clear();
    }
}
