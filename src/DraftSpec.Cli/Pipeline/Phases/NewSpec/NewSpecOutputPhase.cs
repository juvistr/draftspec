namespace DraftSpec.Cli.Pipeline.Phases.NewSpec;

/// <summary>
/// Generates a new spec file from template.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>Items[ProjectPath]</c> - directory where spec will be created</para>
/// <para><b>Reads:</b> <c>Items[SpecName]</c> - name for the new spec (without extension)</para>
/// <para><b>Short-circuits:</b> If name is invalid, directory not found, or file already exists</para>
/// </remarks>
public class NewSpecOutputPhase : ICommandPhase
{
    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var projectPath = context.Get<string>(ContextKeys.ProjectPath);

        if (string.IsNullOrEmpty(projectPath))
        {
            context.Console.WriteError("ProjectPath not set");
            return Task.FromResult(1);
        }

        var name = context.Get<string>(ContextKeys.SpecName);

        if (string.IsNullOrEmpty(name))
        {
            context.Console.WriteError("Usage: draftspec new <Name>");
            return Task.FromResult(1);
        }

        // Security: Validate spec name doesn't contain path separators
        if (!PathValidator.TryValidateFileName(name, out var error))
        {
            context.Console.WriteError(error!);
            return Task.FromResult(1);
        }

        var specHelperPath = Path.Combine(projectPath, "spec_helper.csx");
        if (!context.FileSystem.FileExists(specHelperPath))
        {
            context.Console.WriteWarning("Warning: spec_helper.csx not found. Run 'draftspec init' first.");
        }

        var specPath = Path.Combine(projectPath, $"{name}.spec.csx");
        if (context.FileSystem.FileExists(specPath))
        {
            context.Console.WriteError($"{name}.spec.csx already exists");
            return Task.FromResult(1);
        }

        var specContent = GenerateSpec(name);
        context.FileSystem.WriteAllText(specPath, specContent);
        context.Console.WriteSuccess($"Created {name}.spec.csx");

        return pipeline(context, ct);
    }

    private static string GenerateSpec(string name)
    {
        return $$"""
                 #load "spec_helper.csx"
                 using static DraftSpec.Dsl;

                 describe("{{name}}", () => {
                     it("works"); // Pending spec - add implementation
                 });
                 """;
    }
}
