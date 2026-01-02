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

    #region Record Types

    [Test]
    public async Task HandlesRecordTypes()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public record Person
            {
                public string GetFullName() => "";
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].ClassName).IsEqualTo("Person");
        await Assert.That(methods[0].MethodName).IsEqualTo("GetFullName");
    }

    [Test]
    public async Task HandlesRecordStructTypes()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public record struct Point
            {
                public double Distance() => 0;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].ClassName).IsEqualTo("Point");
    }

    [Test]
    public async Task IgnoresPrivateRecord()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Outer
            {
                private record Inner
                {
                    public void InnerMethod() { }
                }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(0);
    }

    #endregion

    #region Struct Types

    [Test]
    public async Task HandlesStructTypes()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public struct Vector
            {
                public double Magnitude() => 0;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].ClassName).IsEqualTo("Vector");
        await Assert.That(methods[0].MethodName).IsEqualTo("Magnitude");
    }

    [Test]
    public async Task IgnoresPrivateStruct()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Outer
            {
                private struct Inner
                {
                    public void InnerMethod() { }
                }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(0);
    }

    #endregion

    #region Interface Handling

    [Test]
    public async Task IgnoresInterfaceMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public interface IService
            {
                void DoWork();
                Task<string> GetAsync(int id);
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert - interfaces are skipped
        await Assert.That(methods).Count().IsEqualTo(0);
    }

    #endregion

    #region Namespace Handling

    [Test]
    public async Task HandlesBlockScopedNamespace()
    {
        // Arrange
        var source = """
            namespace TestNamespace.SubNamespace
            {
                public class Service
                {
                    public void DoWork() { }
                }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Namespace).IsEqualTo("TestNamespace.SubNamespace");
        await Assert.That(methods[0].FullyQualifiedName).IsEqualTo("TestNamespace.SubNamespace.Service.DoWork");
    }

    [Test]
    public async Task HandlesNoNamespace()
    {
        // Arrange (global namespace)
        var source = """
            public class GlobalService
            {
                public void DoWork() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Namespace).IsEqualTo("");
        await Assert.That(methods[0].FullyQualifiedName).IsEqualTo("GlobalService.DoWork");
    }

    #endregion

    #region Internal Types

    [Test]
    public async Task IncludesInternalClassMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            internal class InternalService
            {
                public void DoWork() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert - internal classes are included as they may be testable
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].ClassName).IsEqualTo("InternalService");
    }

    #endregion

    #region Parameter Type Handling

    [Test]
    public async Task SimplifiesNullableParameterTypes()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void Process(string? name) { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("Process(string)");
    }

    [Test]
    public async Task HandlesGenericParameterTypes()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;

            namespace TestNamespace;

            public class Service
            {
                public void ProcessItems(List<string> items) { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("ProcessItems(List<string>)");
    }

    [Test]
    public async Task ExtractsParameterTypes()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void Process(int id, string name, bool active) { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].ParameterTypes).Count().IsEqualTo(3);
        await Assert.That(methods[0].ParameterTypes[0]).IsEqualTo("int");
        await Assert.That(methods[0].ParameterTypes[1]).IsEqualTo("string");
        await Assert.That(methods[0].ParameterTypes[2]).IsEqualTo("bool");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task HandlesMethodWithNoParameters()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void NoParams() { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("NoParams()");
        await Assert.That(methods[0].ParameterTypes).Count().IsEqualTo(0);
    }

    [Test]
    public async Task HandlesMultipleGenericTypeParameters()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Repository
            {
                public TResult Map<TSource, TResult>(TSource source) => default!;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].Signature).IsEqualTo("Map<TSource, TResult>(TSource)");
    }

    [Test]
    public async Task IgnoresPrivateNestedClass()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Outer
            {
                public void OuterMethod() { }

                private class PrivateInner
                {
                    public void ShouldBeIgnored() { }
                }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].MethodName).IsEqualTo("OuterMethod");
    }

    [Test]
    public async Task HandlesStaticMethods()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Utility
            {
                public static int Calculate(int x) => x * 2;
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].MethodName).IsEqualTo("Calculate");
    }

    [Test]
    public async Task NonExtensionMethodIsNotExtension()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public void RegularMethod(string value) { }
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].IsExtensionMethod).IsFalse();
    }

    [Test]
    public async Task NonAsyncMethodIsNotAsync()
    {
        // Arrange
        var source = """
            namespace TestNamespace;

            public class Service
            {
                public string Get(int id) => "";
            }
            """;

        // Act
        var methods = ParseMethods(source);

        // Assert
        await Assert.That(methods).Count().IsEqualTo(1);
        await Assert.That(methods[0].IsAsync).IsFalse();
    }

    #endregion

    private static List<SourceMethod> ParseMethods(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetCompilationUnitRoot();

        var walker = new SourceMethodWalker("/test/file.cs");
        walker.Visit(root);

        return walker.Methods.ToList();
    }
}
