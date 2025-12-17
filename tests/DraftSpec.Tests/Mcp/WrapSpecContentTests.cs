using DraftSpec.Mcp.Services;

namespace DraftSpec.Tests.Mcp;

/// <summary>
/// Tests for spec content wrapping in MCP service.
/// </summary>
public class WrapSpecContentTests
{
    #region Basic Wrapping

    [Test]
    public async Task WrapSpecContent_AddsPackageDirective()
    {
        var content = """describe("test", () => { it("works", () => {}); });""";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("#:package DraftSpec@*");
    }

    [Test]
    public async Task WrapSpecContent_AddsUsingDirective()
    {
        var content = """describe("test", () => { it("works", () => {}); });""";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task WrapSpecContent_AddsRunCall()
    {
        var content = """describe("test", () => { it("works", () => {}); });""";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("run(json: true);");
    }

    [Test]
    public async Task WrapSpecContent_PreservesUserContent()
    {
        var content = """describe("Calculator", () => { it("adds", () => { expect(1+1).toBe(2); }); });""";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("Calculator");
        await Assert.That(wrapped).Contains("adds");
        await Assert.That(wrapped).Contains("expect(1+1).toBe(2)");
    }

    #endregion

    #region Removing Existing Boilerplate

    [Test]
    public async Task WrapSpecContent_RemovesExistingPackageDirective()
    {
        var content = """
            #:package DraftSpec@1.0.0
            describe("test", () => {});
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // Should only have one package directive (the template's)
        var packageCount = wrapped.Split("#:package DraftSpec@*").Length - 1;
        await Assert.That(packageCount).IsEqualTo(1);

        // Original directive should be commented out
        await Assert.That(wrapped).Contains("// (package directive handled by server)");
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingUsingDirective()
    {
        var content = """
            using static DraftSpec.Dsl;
            describe("test", () => {});
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // Original using should be commented out
        await Assert.That(wrapped).Contains("// (using directive handled by server)");
    }

    [Test]
    public async Task WrapSpecContent_RemovesSimpleRunCall()
    {
        var content = """
            describe("test", () => {});
            run();
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("// (run handled by server)");
    }

    [Test]
    public async Task WrapSpecContent_RemovesRunCallWithJsonArg()
    {
        var content = """
            describe("test", () => {});
            run(json: true);
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // The user's run(json: true) should be commented out
        await Assert.That(wrapped).Contains("// (run handled by server)");

        // The template's run(json: true) should be present at the end
        await Assert.That(wrapped.TrimEnd()).EndsWith("run(json: true);");
    }

    [Test]
    public async Task WrapSpecContent_RemovesRunCallWithSpaces()
    {
        var content = """
            describe("test", () => {});
            run(  json:   true  );
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("// (run handled by server)");
    }

    #endregion

    #region RunCallPattern Regex

    [Test]
    public async Task RunCallPattern_MatchesSimpleRun()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        await Assert.That(pattern.IsMatch("run();")).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_MatchesRunWithArgs()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        await Assert.That(pattern.IsMatch("run(json: true);")).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_MatchesRunWithSpaces()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        await Assert.That(pattern.IsMatch("run  (  );")).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_DoesNotMatchRunWithoutSemicolon()
    {
        var pattern = SpecExecutionService.RunCallPattern();
        var code = """it("should run() something", () => {});""";

        // The pattern requires a semicolon after run(), so run() inside
        // a string without trailing semicolon won't match
        await Assert.That(pattern.IsMatch(code)).IsFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task WrapSpecContent_HandlesEmptyContent()
    {
        var wrapped = SpecExecutionService.WrapSpecContent("");

        await Assert.That(wrapped).Contains("#:package DraftSpec@*");
        await Assert.That(wrapped).Contains("run(json: true);");
    }

    [Test]
    public async Task WrapSpecContent_HandlesMultilineContent()
    {
        var content = """
            describe("Calculator", () => {
                describe("add", () => {
                    it("adds two numbers", () => {
                        expect(1 + 2).toBe(3);
                    });
                });
            });
            """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("Calculator");
        await Assert.That(wrapped).Contains("add");
        await Assert.That(wrapped).Contains("adds two numbers");
    }

    #endregion
}
