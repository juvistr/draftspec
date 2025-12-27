using System.Collections.Concurrent;

namespace DraftSpec.Coverage;

/// <summary>
/// In-process coverage tracker for per-spec coverage tracking.
/// Uses thread-safe collections for parallel spec execution.
/// </summary>
/// <remarks>
/// This tracker requires instrumented assemblies or integration with
/// a coverage tool's programmatic API. In standalone mode, it provides
/// the infrastructure for tracking coverage deltas between spec executions.
///
/// For real coverage data, integrate with:
/// - Coverlet's programmatic API (coverlet.core)
/// - dotnet-coverage's in-process mode
/// - Custom IL instrumentation
/// </remarks>
public class InProcessCoverageTracker : ICoverageTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, int>> _coverage = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, (int covered, int total)>> _branchCoverage = new();
    private readonly object _snapshotLock = new();
    private long _snapshotCounter;
    private bool _isActive;
    private bool _isDisposed;

    /// <inheritdoc />
    public bool IsActive => _isActive;

    /// <inheritdoc />
    public void Start()
    {
        ThrowIfDisposed();
        _isActive = true;
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfDisposed();
        _isActive = false;
    }

    /// <inheritdoc />
    public CoverageSnapshot TakeSnapshot()
    {
        ThrowIfDisposed();

        lock (_snapshotLock)
        {
            var snapshotId = ++_snapshotCounter;

            // Capture current state
            var state = new Dictionary<string, Dictionary<int, int>>();
            foreach (var (file, lines) in _coverage)
            {
                state[file] = new Dictionary<int, int>(lines);
            }

            return new CoverageSnapshot
            {
                Id = snapshotId,
                Timestamp = DateTime.UtcNow,
                State = state
            };
        }
    }

    /// <inheritdoc />
    public SpecCoverageData GetCoverageSince(CoverageSnapshot snapshot, string specId)
    {
        ThrowIfDisposed();

        var previousState = snapshot.State as Dictionary<string, Dictionary<int, int>>
                            ?? new Dictionary<string, Dictionary<int, int>>();

        var filesCovered = new Dictionary<string, CoveredFile>();
        var totalLinesCovered = 0;
        var totalBranchesCovered = 0;

        foreach (var (filePath, currentLines) in _coverage)
        {
            previousState.TryGetValue(filePath, out var previousLines);
            previousLines ??= new Dictionary<int, int>();

            var deltaHits = new Dictionary<int, int>();

            foreach (var (lineNumber, currentHits) in currentLines)
            {
                previousLines.TryGetValue(lineNumber, out var previousHits);
                var delta = currentHits - previousHits;

                if (delta > 0)
                {
                    deltaHits[lineNumber] = delta;
                }
            }

            if (deltaHits.Count > 0)
            {
                // Get branch coverage for this file
                Dictionary<int, BranchCoverage>? branchHits = null;
                if (_branchCoverage.TryGetValue(filePath, out var branches))
                {
                    branchHits = branches
                        .Where(b => deltaHits.ContainsKey(b.Key))
                        .ToDictionary(
                            b => b.Key,
                            b => new BranchCoverage { Covered = b.Value.covered, Total = b.Value.total });

                    totalBranchesCovered += branchHits.Values.Sum(b => b.Covered);
                }

                filesCovered[filePath] = new CoveredFile
                {
                    FilePath = filePath,
                    LineHits = deltaHits,
                    BranchHits = branchHits
                };

                totalLinesCovered += deltaHits.Count;
            }
        }

        return new SpecCoverageData
        {
            SpecId = specId,
            FilesCovered = filesCovered,
            Summary = new SpecCoverageSummary
            {
                LinesCovered = totalLinesCovered,
                BranchesCovered = totalBranchesCovered,
                FilesTouched = filesCovered.Count
            }
        };
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, CoveredFile> GetTotalCoverage()
    {
        ThrowIfDisposed();

        var result = new Dictionary<string, CoveredFile>();

        foreach (var (filePath, lines) in _coverage)
        {
            Dictionary<int, BranchCoverage>? branchHits = null;
            if (_branchCoverage.TryGetValue(filePath, out var branches))
            {
                branchHits = branches.ToDictionary(
                    b => b.Key,
                    b => new BranchCoverage { Covered = b.Value.covered, Total = b.Value.total });
            }

            result[filePath] = new CoveredFile
            {
                FilePath = filePath,
                LineHits = new Dictionary<int, int>(lines),
                BranchHits = branchHits
            };
        }

        return result;
    }

    /// <summary>
    /// Record a line hit. Called by instrumented code.
    /// </summary>
    /// <param name="filePath">Source file path</param>
    /// <param name="lineNumber">Line number that was executed</param>
    public void RecordLineHit(string filePath, int lineNumber)
    {
        if (!_isActive) return;

        var fileLines = _coverage.GetOrAdd(filePath, _ => new ConcurrentDictionary<int, int>());
        fileLines.AddOrUpdate(lineNumber, 1, (_, current) => current + 1);
    }

    /// <summary>
    /// Record branch coverage. Called by instrumented code.
    /// </summary>
    /// <param name="filePath">Source file path</param>
    /// <param name="lineNumber">Line number with branch</param>
    /// <param name="branchesCovered">Number of branches taken</param>
    /// <param name="branchesTotal">Total branches at this point</param>
    public void RecordBranchHit(string filePath, int lineNumber, int branchesCovered, int branchesTotal)
    {
        if (!_isActive) return;

        var fileBranches = _branchCoverage.GetOrAdd(filePath, _ => new ConcurrentDictionary<int, (int, int)>());
        fileBranches.AddOrUpdate(
            lineNumber,
            (branchesCovered, branchesTotal),
            (_, current) => (Math.Max(current.Item1, branchesCovered), branchesTotal));
    }

    /// <summary>
    /// Clear all coverage data.
    /// </summary>
    public void Reset()
    {
        _coverage.Clear();
        _branchCoverage.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _isActive = false;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
