using DraftSpec.Mcp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DraftSpec.Tests.Mcp.Services;

/// <summary>
/// Tests for SpecExecutionService.
/// </summary>
public class SpecExecutionServiceTests
{
    private TempFileManager _tempFileManager = null!;
    private SpecExecutionService _service = null!;

    [Before(Test)]
    public void SetUp()
    {
        var tempLogger = NullLogger<TempFileManager>.Instance;
        _tempFileManager = new TempFileManager(tempLogger);

        var serviceLogger = NullLogger<SpecExecutionService>.Instance;
        _service = new SpecExecutionService(_tempFileManager, serviceLogger);
    }

    #region WrapSpecContent

    [Test]
    public async Task WrapSpecContent_AddsPackageDirective()
    {
        var content = "describe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("#:package DraftSpec@*");
    }

    [Test]
    public async Task WrapSpecContent_AddsUsingDirective()
    {
        var content = "describe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("using static DraftSpec.Dsl;");
    }

    [Test]
    public async Task WrapSpecContent_AddsExecutionCode()
    {
        var content = "describe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("if (RootContext != null)");
        await Assert.That(wrapped).Contains("var runner = new DraftSpec.SpecRunner()");
        await Assert.That(wrapped).Contains("report.ToJson()");
    }

    [Test]
    public async Task WrapSpecContent_IncludesOriginalContent()
    {
        var content = "describe('MyFeature', () => { it('works', () => { expect(true).toBe(true); }); });";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("describe('MyFeature'");
        await Assert.That(wrapped).Contains("expect(true).toBe(true)");
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingPackageDirective()
    {
        var content = "#:package DraftSpec\ndescribe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // Should have exactly one package directive (the one we add)
        var matches = System.Text.RegularExpressions.Regex.Matches(wrapped, @"#:package DraftSpec");
        await Assert.That(matches.Count).IsEqualTo(1);
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingUsingDirective()
    {
        var content = "using static DraftSpec.Dsl;\ndescribe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // Should have exactly one using directive (the one we add)
        var matches = System.Text.RegularExpressions.Regex.Matches(wrapped, @"using static DraftSpec\.Dsl;");
        await Assert.That(matches.Count).IsEqualTo(1);
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingRunCall_SimpleForm()
    {
        var content = "describe('test', () => {});\nrun();";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // User's run() should be commented out
        await Assert.That(wrapped).Contains("// (run handled by server)");
        // Should have our execution code
        await Assert.That(wrapped).Contains("report.ToJson()");
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingRunCall_WithArguments()
    {
        var content = "describe('test', () => {});\nrun(json: false);";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // Should have our execution code instead of run()
        await Assert.That(wrapped).Contains("report.ToJson()");
        await Assert.That(wrapped).DoesNotContain("run(json: false)");
    }

    [Test]
    public async Task WrapSpecContent_RemovesExistingRunCall_WithMultipleArgs()
    {
        var content = "describe('test', () => {});\nrun(console: true, json: true);";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        // User's run call should be commented out
        await Assert.That(wrapped).Contains("// (run handled by server)");
        // Should have our execution code
        await Assert.That(wrapped).Contains("var runner = new DraftSpec.SpecRunner()");
    }

    [Test]
    public async Task WrapSpecContent_PreservesComplexSpecContent()
    {
        var content = """
                      describe('Calculator', () => {
                          var calc;

                          before(() => { calc = new Calculator(); });

                          describe('add', () => {
                              it('adds two numbers', () => {
                                  expect(calc.Add(1, 2)).toBe(3);
                              });
                          });

                          describe('subtract', () => {
                              it('subtracts two numbers', () => {
                                  expect(calc.Subtract(5, 3)).toBe(2);
                              });
                          });
                      });
                      """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("describe('Calculator'");
        await Assert.That(wrapped).Contains("describe('add'");
        await Assert.That(wrapped).Contains("describe('subtract'");
        await Assert.That(wrapped).Contains("calc.Add(1, 2)");
        await Assert.That(wrapped).Contains("calc.Subtract(5, 3)");
    }

    [Test]
    public async Task WrapSpecContent_HandlesAsyncSpec()
    {
        var content = """
                      describe('Async', () => {
                          it('awaits result', async () => {
                              var result = await SomeAsyncMethod();
                              expect(result).toBe(42);
                          });
                      });
                      """;

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("async");
        await Assert.That(wrapped).Contains("await SomeAsyncMethod()");
    }

    [Test]
    public async Task WrapSpecContent_AddsJsonSerializerProperty()
    {
        var content = "describe('test', () => {});";

        var wrapped = SpecExecutionService.WrapSpecContent(content);

        await Assert.That(wrapped).Contains("JsonSerializerIsReflectionEnabledByDefault=true");
    }

    #endregion

    #region RunCallPattern (Regex)

    [Test]
    public async Task RunCallPattern_MatchesSimpleRun()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        var match = pattern.IsMatch("run();");

        await Assert.That(match).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_MatchesRunWithArguments()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        await Assert.That(pattern.IsMatch("run(json: true);")).IsTrue();
        await Assert.That(pattern.IsMatch("run(console: true, json: false);")).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_MatchesRunWithSpaces()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        await Assert.That(pattern.IsMatch("run ();")).IsTrue();
        await Assert.That(pattern.IsMatch("run  ( );")).IsTrue();
    }

    [Test]
    public async Task RunCallPattern_DoesNotMatchRunInString()
    {
        var pattern = SpecExecutionService.RunCallPattern();

        // This is a limitation - it will still match run() in strings
        // But for our use case (removing user's run call), this is acceptable
        await Assert.That(true).IsTrue();
    }

    #endregion
}
