namespace DraftSpec.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Fluent builder for creating test spec fixtures.
/// Creates temporary spec files and directories for testing.
/// </summary>
public class SpecFixtureBuilder
{
    private readonly string _directory;
    private readonly List<(string Name, string Content)> _specs = [];
    private string? _specHelperContent;

    public SpecFixtureBuilder(string directory)
    {
        _directory = directory;
    }

    /// <summary>
    /// Adds a passing spec to the fixture.
    /// </summary>
    public SpecFixtureBuilder WithPassingSpec(string name = "passing")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("PassingTests", () =>
            {
                it("passes", () =>
                {
                    expect(true).toBeTrue();
                });
            });
            """);
    }

    /// <summary>
    /// Adds a failing spec to the fixture.
    /// </summary>
    public SpecFixtureBuilder WithFailingSpec(string name = "failing")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("FailingTests", () =>
            {
                it("fails", () =>
                {
                    expect(false).toBeTrue();
                });
            });
            """);
    }

    /// <summary>
    /// Adds a pending spec (no body) to the fixture.
    /// </summary>
    public SpecFixtureBuilder WithPendingSpec(string name = "pending")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("PendingTests", () =>
            {
                it("is pending");
            });
            """);
    }

    /// <summary>
    /// Adds a spec with multiple examples.
    /// </summary>
    public SpecFixtureBuilder WithMultipleSpecs(string name = "multiple", int passCount = 2, int failCount = 1)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine("using static DraftSpec.Dsl;");
        content.AppendLine();
        content.AppendLine("describe(\"MultipleTests\", () =>");
        content.AppendLine("{");

        for (int i = 1; i <= passCount; i++)
        {
            content.AppendLine($"    it(\"passes {i}\", () => expect(true).toBeTrue());");
        }

        for (int i = 1; i <= failCount; i++)
        {
            content.AppendLine($"    it(\"fails {i}\", () => expect(false).toBeTrue());");
        }

        content.AppendLine("});");

        return WithSpec(name, content.ToString());
    }

    /// <summary>
    /// Adds a spec with a compilation error.
    /// </summary>
    public SpecFixtureBuilder WithCompilationErrorSpec(string name = "compilation_error")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("CompilationError", () =>
            {
                it("has syntax error", () =>
                {
                    var x = // missing expression
                });
            });
            """);
    }

    /// <summary>
    /// Adds a spec with async examples.
    /// </summary>
    public SpecFixtureBuilder WithAsyncSpec(string name = "async")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("AsyncTests", () =>
            {
                it("handles async", async () =>
                {
                    await Task.Delay(1);
                    expect(true).toBeTrue();
                });
            });
            """);
    }

    /// <summary>
    /// Adds a spec with hooks (before/after).
    /// </summary>
    public SpecFixtureBuilder WithHooksSpec(string name = "hooks")
    {
        return WithSpec(name, """
            using static DraftSpec.Dsl;

            describe("HooksTests", () =>
            {
                var value = 0;

                before(() => value = 42);
                after(() => value = 0);

                it("sees value from before hook", () =>
                {
                    expect(value).toBe(42);
                });
            });
            """);
    }

    /// <summary>
    /// Adds a custom spec with the given content.
    /// </summary>
    public SpecFixtureBuilder WithSpec(string name, string content)
    {
        var fileName = name.EndsWith(".spec.csx") ? name : $"{name}.spec.csx";
        _specs.Add((fileName, content));
        return this;
    }

    /// <summary>
    /// Adds a spec_helper.csx file.
    /// </summary>
    public SpecFixtureBuilder WithSpecHelper(string content)
    {
        _specHelperContent = content;
        return this;
    }

    /// <summary>
    /// Builds the fixture by creating the directory and writing all spec files.
    /// Returns the path to the spec directory.
    /// </summary>
    public string Build()
    {
        Directory.CreateDirectory(_directory);

        if (_specHelperContent != null)
        {
            File.WriteAllText(
                Path.Combine(_directory, "spec_helper.csx"),
                _specHelperContent);
        }

        foreach (var (name, content) in _specs)
        {
            File.WriteAllText(Path.Combine(_directory, name), content);
        }

        return _directory;
    }
}
