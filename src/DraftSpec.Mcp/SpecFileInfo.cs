using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace DraftSpec.Mcp.Resources;

/// <summary>
/// Information about a spec file.
/// </summary>
internal class SpecFileInfo
{
    public string Path { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public DateTime ModifiedAt { get; set; }
}
