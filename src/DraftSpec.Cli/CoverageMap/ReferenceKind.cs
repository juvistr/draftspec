namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// The kind of type reference found in code.
/// </summary>
public enum ReferenceKind
{
    /// <summary>Object instantiation: new T()</summary>
    New,

    /// <summary>Type cast: (T)expr</summary>
    Cast,

    /// <summary>typeof expression: typeof(T)</summary>
    TypeOf,

    /// <summary>Generic type argument: List&lt;T&gt;</summary>
    Generic,

    /// <summary>Variable declaration: T variable</summary>
    Variable,

    /// <summary>Other reference</summary>
    Other
}
