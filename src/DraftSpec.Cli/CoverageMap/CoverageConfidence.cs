namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// The confidence level of spec coverage for a method.
/// </summary>
public enum CoverageConfidence
{
    /// <summary>No coverage detected.</summary>
    None = 0,

    /// <summary>Only namespace match via using directive.</summary>
    Low = 1,

    /// <summary>Type instantiation or reference in spec body.</summary>
    Medium = 2,

    /// <summary>Direct method call in spec body.</summary>
    High = 3
}
