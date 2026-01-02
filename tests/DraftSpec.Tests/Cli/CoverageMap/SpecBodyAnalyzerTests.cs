using DraftSpec.Cli.CoverageMap;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class SpecBodyAnalyzerTests
{
    [Test]
    public async Task ExtractsMethodCalls()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                it("creates a user", () =>
                {
                    var service = new UserService();
                    service.CreateAsync("test@example.com");
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].MethodCalls).Count().IsEqualTo(1);
        await Assert.That(specs[0].MethodCalls[0].MethodName).IsEqualTo("CreateAsync");
    }

    [Test]
    public async Task ExtractsTypeInstantiations()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("TodoService", () =>
            {
                it("creates a todo", () =>
                {
                    var service = new TodoService();
                    var todo = new Todo();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(2);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("TodoService");
        await Assert.That(specs[0].TypeReferences[0].Kind).IsEqualTo(ReferenceKind.New);
        await Assert.That(specs[0].TypeReferences[1].TypeName).IsEqualTo("Todo");
    }

    [Test]
    public async Task ExtractsUsingDirectives()
    {
        // Arrange
        var source = """
            using MyApp.Services;
            using MyApp.Models;
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("does something", () => { });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].UsingNamespaces).Contains("MyApp.Services");
        await Assert.That(specs[0].UsingNamespaces).Contains("MyApp.Models");
    }

    [Test]
    public async Task HandlesNestedContexts()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Parent", () =>
            {
                describe("Child", () =>
                {
                    it("nested spec", () =>
                    {
                        new Service();
                    });
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].ContextPath).Count().IsEqualTo(2);
        await Assert.That(specs[0].ContextPath[0]).IsEqualTo("Parent");
        await Assert.That(specs[0].ContextPath[1]).IsEqualTo("Child");
    }

    [Test]
    public async Task IgnoresHookBodies()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                before(() =>
                {
                    new SetupService();
                });

                it("test spec", () =>
                {
                    new TestService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        // Should only have TestService, not SetupService
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("TestService");
    }

    [Test]
    public async Task HandlesFocusedSpecs()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                fit("focused spec", () =>
                {
                    new FocusedService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecDescription).IsEqualTo("focused spec");
    }

    [Test]
    public async Task SkipsPendingSpecs()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("pending spec");
                it("implemented spec", () =>
                {
                    new Service();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        // Pending spec should be skipped (no body to analyze)
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecDescription).IsEqualTo("implemented spec");
    }

    [Test]
    public async Task ExtractsTypeOfReferences()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("checks type", () =>
                {
                    var type = typeof(UserService);
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("UserService");
        await Assert.That(specs[0].TypeReferences[0].Kind).IsEqualTo(ReferenceKind.TypeOf);
    }

    [Test]
    public async Task GeneratesCorrectSpecId()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                describe("CreateAsync", () =>
                {
                    it("creates a user", () => { });
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source, "/project", "/project/specs/user.spec.csx");

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecId).IsEqualTo("specs/user.spec.csx:UserService/CreateAsync/creates a user");
    }

    #region Cast Expressions

    [Test]
    public async Task ExtractsCastExpressions()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("casts object", () =>
                {
                    object obj = new object();
                    var user = (UserService)obj;
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var castRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.Cast);
        await Assert.That(castRef).IsNotNull();
        await Assert.That(castRef!.TypeName).IsEqualTo("UserService");
    }

    #endregion

    #region Variable Declarations

    [Test]
    public async Task ExtractsExplicitTypeVariableDeclarations()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("declares variable", () =>
                {
                    UserService service = null!;
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var varRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.Variable);
        await Assert.That(varRef).IsNotNull();
        await Assert.That(varRef!.TypeName).IsEqualTo("UserService");
    }

    [Test]
    public async Task IgnoresVarDeclarations()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("uses var", () =>
                {
                    var count = 0;
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var varRefs = specs[0].TypeReferences.Where(t => t.Kind == ReferenceKind.Variable).ToList();
        // "var" should be skipped
        await Assert.That(varRefs).Count().IsEqualTo(0);
    }

    #endregion

    #region Context Aliases

    [Test]
    public async Task HandlesContextBlock()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                context("when user exists", () =>
                {
                    it("returns user", () =>
                    {
                        new UserService();
                    });
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].ContextPath).Count().IsEqualTo(2);
        await Assert.That(specs[0].ContextPath[1]).IsEqualTo("when user exists");
    }

    [Test]
    public async Task HandlesFdescribeBlock()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            fdescribe("Focused describe", () =>
            {
                it("runs focused spec", () =>
                {
                    new FocusedService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].ContextPath[0]).IsEqualTo("Focused describe");
    }

    [Test]
    public async Task HandlesXdescribeBlock()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            xdescribe("Skipped describe", () =>
            {
                it("skipped spec", () =>
                {
                    new SkippedService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].ContextPath[0]).IsEqualTo("Skipped describe");
    }

    #endregion

    #region Hook Types

    [Test]
    public async Task IgnoresBeforeAllHook()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                beforeAll(() =>
                {
                    new SetupService();
                });

                it("test spec", () =>
                {
                    new TestService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        // Should only have TestService, not SetupService from beforeAll
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "SetupService")).IsFalse();
    }

    [Test]
    public async Task IgnoresAfterAllHook()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                afterAll(() =>
                {
                    new CleanupService();
                });

                it("test spec", () =>
                {
                    new TestService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "CleanupService")).IsFalse();
    }

    [Test]
    public async Task IgnoresAfterHook()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                after(() =>
                {
                    new TeardownService();
                });

                it("test spec", () =>
                {
                    new TestService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "TeardownService")).IsFalse();
    }

    #endregion

    #region Method Call Receiver Extraction

    [Test]
    public async Task ExtractsMethodCallReceiver()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("calls method on receiver", () =>
                {
                    var service = new UserService();
                    service.CreateUser();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var methodCall = specs[0].MethodCalls.FirstOrDefault(m => m.MethodName == "CreateUser");
        await Assert.That(methodCall).IsNotNull();
        await Assert.That(methodCall!.ReceiverName).IsEqualTo("service");
    }

    [Test]
    public async Task RecordsLineNumberForMethodCalls()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("tracks lines", () =>
                {
                    var service = new UserService();
                    service.DoWork();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var methodCall = specs[0].MethodCalls.FirstOrDefault(m => m.MethodName == "DoWork");
        await Assert.That(methodCall).IsNotNull();
        await Assert.That(methodCall!.LineNumber).IsGreaterThan(0);
    }

    #endregion

    #region Type Name Extraction Edge Cases

    [Test]
    public async Task HandlesGenericTypeCreation()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("creates generic", () =>
                {
                    var list = new List<string>();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var typeRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.New);
        await Assert.That(typeRef).IsNotNull();
        await Assert.That(typeRef!.TypeName).IsEqualTo("List");
    }

    [Test]
    public async Task HandlesQualifiedTypeName()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("uses qualified name", () =>
                {
                    var type = typeof(System.String);
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var typeRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.TypeOf);
        await Assert.That(typeRef).IsNotNull();
        await Assert.That(typeRef!.TypeName).IsEqualTo("String");
    }

    [Test]
    public async Task HandlesNullableTypeInTypeOf()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("checks nullable type", () =>
                {
                    var type = typeof(int?);
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var typeRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.TypeOf);
        await Assert.That(typeRef).IsNotNull();
        await Assert.That(typeRef!.TypeName).IsEqualTo("int");
    }

    [Test]
    public async Task HandlesArrayTypeInTypeOf()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("checks array type", () =>
                {
                    var type = typeof(string[]);
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        var typeRef = specs[0].TypeReferences.FirstOrDefault(t => t.Kind == ReferenceKind.TypeOf);
        await Assert.That(typeRef).IsNotNull();
        await Assert.That(typeRef!.TypeName).IsEqualTo("string");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task HandlesSpecWithNoMethodCalls()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("simple assertion", () =>
                {
                    var x = 1 + 1;
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].MethodCalls).Count().IsEqualTo(0);
    }

    [Test]
    public async Task HandlesMultipleSpecsInSameContext()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("first spec", () => { new Service1(); });
                it("second spec", () => { new Service2(); });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(2);
        await Assert.That(specs[0].SpecDescription).IsEqualTo("first spec");
        await Assert.That(specs[1].SpecDescription).IsEqualTo("second spec");
    }

    [Test]
    public async Task UsingNamespacesAreSharedAcrossSpecs()
    {
        // Arrange
        var source = """
            using MyApp.Services;
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("first spec", () => { });
                it("second spec", () => { });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(2);
        await Assert.That(specs[0].UsingNamespaces).Contains("MyApp.Services");
        await Assert.That(specs[1].UsingNamespaces).Contains("MyApp.Services");
    }

    [Test]
    public async Task RecordsLineNumberForTypeReferences()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("creates object", () =>
                {
                    new Service();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences).Count().IsGreaterThanOrEqualTo(1);
        await Assert.That(specs[0].TypeReferences[0].LineNumber).IsGreaterThan(0);
    }

    #endregion

    #region Lambda Expression Variants

    [Test]
    public async Task HandlesSimpleLambdaInSpec()
    {
        // Arrange - simple lambda: x => x.Method()
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("uses simple lambda", () =>
                {
                    var items = new[] { 1, 2, 3 };
                    items.Select(x => x.ToString());
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        // The ToString method call inside the simple lambda should be extracted
        await Assert.That(specs[0].MethodCalls.Any(m => m.MethodName == "ToString")).IsTrue();
    }

    [Test]
    public async Task HandlesSimpleLambdaWithTypeCreation()
    {
        // Arrange - simple lambda with new expression
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("creates in simple lambda", () =>
                {
                    var items = new[] { "a", "b" };
                    items.Select(s => new Wrapper(s));
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "Wrapper")).IsTrue();
    }

    [Test]
    public async Task HandlesAnonymousMethodInSpec()
    {
        // Arrange - delegate { } syntax
        var source = """
            using static DraftSpec.Dsl;
            using System;

            describe("Test", () =>
            {
                it("uses anonymous method", () =>
                {
                    Action action = delegate
                    {
                        var service = new AnonymousService();
                        service.Execute();
                    };
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "AnonymousService")).IsTrue();
        await Assert.That(specs[0].MethodCalls.Any(m => m.MethodName == "Execute")).IsTrue();
    }

    [Test]
    public async Task HandlesAnonymousMethodWithParameter()
    {
        // Arrange - delegate(x) { } syntax
        var source = """
            using static DraftSpec.Dsl;
            using System;

            describe("Test", () =>
            {
                it("uses anonymous method with param", () =>
                {
                    Action<string> action = delegate(string s)
                    {
                        new Processor().Process(s);
                    };
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences.Any(t => t.TypeName == "Processor")).IsTrue();
        await Assert.That(specs[0].MethodCalls.Any(m => m.MethodName == "Process")).IsTrue();
    }

    #endregion

    private static List<SpecReference> ParseSpecs(
        string source,
        string projectPath = "/project",
        string sourceFile = "/project/test.spec.csx")
    {
        // Strip #load and #r directives for parsing
        var lines = source.Split('\n').Where(line =>
        {
            var trimmed = line.TrimStart();
            return !trimmed.StartsWith("#load", StringComparison.Ordinal) &&
                   !trimmed.StartsWith("#r", StringComparison.Ordinal);
        });
        var cleanSource = string.Join('\n', lines);

        var parseOptions = CSharpParseOptions.Default
            .WithKind(Microsoft.CodeAnalysis.SourceCodeKind.Script);
        var syntaxTree = CSharpSyntaxTree.ParseText(cleanSource, parseOptions);
        var root = syntaxTree.GetCompilationUnitRoot();

        var analyzer = new SpecBodyAnalyzer(sourceFile, projectPath);
        analyzer.Visit(root);

        return analyzer.SpecReferences.ToList();
    }
}
