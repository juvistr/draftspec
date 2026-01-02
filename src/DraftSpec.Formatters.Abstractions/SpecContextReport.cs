namespace DraftSpec.Formatters.Abstractions;

/// <summary>
/// A context (describe block) containing specs and nested contexts.
/// </summary>
public class SpecContextReport
{
    public string Description { get; set; } = "";
    public IList<SpecResultReport> Specs { get; set; } = [];
    public IList<SpecContextReport> Contexts { get; set; } = [];
}
