namespace DraftSpec.Mcp.Models;

/// <summary>
/// Recursive structure representing a describe block with specs and nested contexts.
/// Used by the scaffold_specs tool to generate DraftSpec code.
/// </summary>
public class ScaffoldNode
{
    /// <summary>
    /// Description for the describe block.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// List of spec descriptions (it blocks) at this level.
    /// </summary>
    public List<string> Specs { get; set; } = [];

    /// <summary>
    /// Nested describe blocks.
    /// </summary>
    public List<ScaffoldNode> Contexts { get; set; } = [];
}
