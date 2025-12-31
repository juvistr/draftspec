namespace DraftSpec.Formatters.Abstractions;

/// <summary>
/// A context (describe block) containing specs and nested contexts.
/// </summary>
public class SpecContextReport
{
    public string Description { get; set; } = "";
    public List<SpecResultReport> Specs { get; set; } = [];
    public List<SpecContextReport> Contexts { get; set; } = [];
}