namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Loads project configuration and applies defaults to CLI options.
/// </summary>
public class ConfigApplier : IConfigApplier
{
    private readonly IConfigLoader _configLoader;

    public ConfigApplier(IConfigLoader configLoader)
    {
        _configLoader = configLoader;
    }

    /// <inheritdoc />
    public void ApplyConfig(CliOptions options)
    {
        var result = _configLoader.Load(options.Path);

        if (result.Error != null)
            throw new InvalidOperationException(result.Error);

        if (result.Config != null)
            options.ApplyDefaults(result.Config);
    }
}
