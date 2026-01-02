using DraftSpec.Cli.CoverageMap;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class SourceMethodWalkerTests
{
    [Test]
    public async Task ParsesPublicMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Calculator
            {
                public int Add(int a, int b) => a + b;
                public int Subtract(int a, int b) => a - b;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(2);
        await Assert.That(methods[0].MethodName).IsEqualTo("Add");
        await Assert.That(methods[1].MethodName).IsEqualTo("Subtract");
    }

    [Test]
    public async Task IgnoresPrivateMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void PublicMethod() { }
                private void PrivateMethod() { }
                internal void InternalMethod() { }
                protected void ProtectedMethod() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].MethodName).IsEqualTo("PublicMethod");
    }

    [Test]
    public async Task ExtractsMethodSignature()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public string GetName(int id, bool includeDetails) => "";
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("GetName(int, bool)");
    }

    [Test]
    public async Task HandlesAsyncMethods()
    {
        // Arrange
        var source = """
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class Service
            {
                public async Task<string> GetAsync(int id) => await Task.FromResult("");
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].IsAsync).IsTrue();
        await Assert.That(methods[0].MethodName).IsEqualTo("GetAsync");
    }

    [Test]
    public async Task ExtractsFullyQualifiedName()
    {
        // Arrange
        var source = """
            namespace MyApp.Services;

            public class UserService
            {
                public void CreateUser() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].FullyQualifiedName).IsEqualTo("MyApp.Services.UserService.CreateUser");
        await Assert.That(methods[0].Namespace).IsEqualTo("MyApp.Services");
        await Assert.That(methods[0].ClassName).IsEqualTo("UserService");
    }

    [Test]
    public async Task HandlesNestedClasses()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Outer
            {
                public void OuterMethod() { }

                public class Inner
                {
                    public void InnerMethod() { }
                }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(2);
        await Assert.That(methods[0].ClassName).IsEqualTo("Outer");
        await Assert.That(methods[1].ClassName).IsEqualTo("Inner");
    }

    [Test]
    public async Task HandlesGenericMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Repository
            {
                public T Get<T>(int id) where T : class => default!;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("Get<T>(int)");
    }

    [Test]
    public async Task CapturesLineNumber()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void FirstMethod() { }

                public void SecondMethod() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(2);
        await Assert.That(methods[0].LineNumber).IsEqualTo(5);
        await Assert.That(methods[1].LineNumber).IsEqualTo(7);
    }

    [Test]
    public async Task DetectsExtensionMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public static class StringExtensions
            {
                public static string ToUpperFirst(this string value) => value;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].IsExtensionMethod).IsTrue();
    }

    private static List<SourceMethod> ParseMethods(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetCompilationUnitRoot();

        var walker = new SourceMethodWalker("/test/file.cs");
        walker.Visit(root);

        return walker.Methods.ToList();
    }
}
