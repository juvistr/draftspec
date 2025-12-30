namespace DraftSpec.Cli;

/// <summary>
/// Pre-run statistics about discovered specs.
/// Used to display summary before test execution.
/// </summary>
/// <param name="Total">Total number of specs discovered.</param>
/// <param name="Regular">Number of regular specs (not focused, skipped, or pending).</param>
/// <param name="Focused">Number of focused specs (fit/fdescribe).</param>
/// <param name="Skipped">Number of skipped specs (xit/xdescribe).</param>
/// <param name="Pending">Number of pending specs (no body).</param>
/// <param name="HasFocusMode">True if any focused specs exist (focus mode active).</param>
/// <param name="FileCount">Number of spec files.</param>
public record SpecStats(
    int Total,
    int Regular,
    int Focused,
    int Skipped,
    int Pending,
    bool HasFocusMode,
    int FileCount);
