namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Attribute to mark a class as a DraftSpec plugin for auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DraftSpecPluginAttribute : Attribute
{
    /// <summary>
    /// The name used to reference this plugin (e.g., "json", "html").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional description of the plugin.
    /// </summary>
    public string? Description { get; set; }

    public DraftSpecPluginAttribute(string name)
    {
        Name = name;
    }
}