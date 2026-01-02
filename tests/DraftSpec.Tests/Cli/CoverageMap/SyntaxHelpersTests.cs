using DraftSpec.Cli.CoverageMap;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class SyntaxHelpersTests
{
    #region ExtractStringLiteral Tests

    [Test]
    public async Task ExtractStringLiteral_RegularString_ReturnsValue()
    {
        var expression = ParseExpression("\"hello world\"");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task ExtractStringLiteral_VerbatimString_ReturnsValue()
    {
        var expression = ParseExpression("@\"hello\\nworld\"");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsEqualTo("hello\\nworld");
    }

    [Test]
    public async Task ExtractStringLiteral_RawString_ReturnsValue()
    {
        var expression = ParseExpression("\"\"\"raw string\"\"\"");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsEqualTo("raw string");
    }

    [Test]
    public async Task ExtractStringLiteral_Parenthesized_ReturnsValue()
    {
        var expression = ParseExpression("(\"nested\")");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsEqualTo("nested");
    }

    [Test]
    public async Task ExtractStringLiteral_DeeplyParenthesized_ReturnsValue()
    {
        var expression = ParseExpression("((\"deeply nested\"))");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsEqualTo("deeply nested");
    }

    [Test]
    public async Task ExtractStringLiteral_NumericLiteral_ReturnsNull()
    {
        var expression = ParseExpression("42");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ExtractStringLiteral_Identifier_ReturnsNull()
    {
        var expression = ParseExpression("someVariable");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ExtractStringLiteral_BinaryExpression_ReturnsNull()
    {
        var expression = ParseExpression("\"a\" + \"b\"");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ExtractStringLiteral_InterpolatedString_ReturnsNull()
    {
        var expression = ParseExpression("$\"hello {name}\"");

        var result = SyntaxHelpers.ExtractStringLiteral(expression);

        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetMethodName Tests

    [Test]
    public async Task GetMethodName_DirectCall_ReturnsMethodName()
    {
        var invocation = ParseInvocation("DoSomething()");

        var result = SyntaxHelpers.GetMethodName(invocation);

        await Assert.That(result).IsEqualTo("DoSomething");
    }

    [Test]
    public async Task GetMethodName_MemberAccess_ReturnsMethodName()
    {
        var invocation = ParseInvocation("obj.Method()");

        var result = SyntaxHelpers.GetMethodName(invocation);

        await Assert.That(result).IsEqualTo("Method");
    }

    [Test]
    public async Task GetMethodName_ChainedMemberAccess_ReturnsLastMethodName()
    {
        var invocation = ParseInvocation("a.b.c.Method()");

        var result = SyntaxHelpers.GetMethodName(invocation);

        await Assert.That(result).IsEqualTo("Method");
    }

    [Test]
    public async Task GetMethodName_GenericMethod_ReturnsNull()
    {
        // Generic method calls use GenericNameSyntax, not handled
        var invocation = ParseInvocation("Create<T>()");

        var result = SyntaxHelpers.GetMethodName(invocation);

        // GenericNameSyntax is not currently handled, returns null
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetMethodName_ConditionalAccess_ReturnsNull()
    {
        // obj?.Method() is a ConditionalAccessExpressionSyntax, not handled
        var invocation = ParseInvocation("Invoke(obj?.Method())");
        // Get the inner invocation - can't directly parse conditional access as statement
        var innerInvocation = invocation.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Skip(1)
            .FirstOrDefault();

        // ConditionalAccess is not directly parseable as invocation, test the pattern
        // For now, test that an expression syntax we can't handle returns null
        var expr = ParseExpression("obj?.Method");
        var result = SyntaxHelpers.GetMethodName(SyntaxFactory.InvocationExpression(expr));

        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetSimpleTypeName Tests

    [Test]
    public async Task GetSimpleTypeName_Identifier_ReturnsTypeName()
    {
        var type = ParseType("MyClass");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("MyClass");
    }

    [Test]
    public async Task GetSimpleTypeName_Generic_ReturnsTypeName()
    {
        var type = ParseType("List<int>");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("List");
    }

    [Test]
    public async Task GetSimpleTypeName_Qualified_ReturnsRightMostName()
    {
        var type = ParseType("System.Collections.Generic.List<int>");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("List");
    }

    [Test]
    public async Task GetSimpleTypeName_Nullable_ReturnsUnderlyingTypeName()
    {
        var type = ParseType("int?");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task GetSimpleTypeName_Array_ReturnsElementTypeName()
    {
        var type = ParseType("string[]");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("string");
    }

    [Test]
    public async Task GetSimpleTypeName_Predefined_ReturnsKeyword()
    {
        var type = ParseType("int");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task GetSimpleTypeName_PredefinedString_ReturnsKeyword()
    {
        var type = ParseType("string");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("string");
    }

    [Test]
    public async Task GetSimpleTypeName_NestedNullableGeneric_ReturnsTypeName()
    {
        var type = ParseType("Dictionary<string, int>?");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("Dictionary");
    }

    [Test]
    public async Task GetSimpleTypeName_ArrayOfNullable_ReturnsElementTypeName()
    {
        var type = ParseType("int?[]");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task GetSimpleTypeName_TupleType_ReturnsNull()
    {
        // Tuple types are not handled, should return null
        var type = ParseType("(int, string)");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSimpleTypeName_PointerType_ReturnsNull()
    {
        // Pointer types are not handled, should return null
        var type = ParseTypeFromUnsafe("int*");

        var result = SyntaxHelpers.GetSimpleTypeName(type);

        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetLineNumber Tests

    [Test]
    public async Task GetLineNumber_SingleLineExpression_ReturnsCorrectLine()
    {
        var tree = CSharpSyntaxTree.ParseText("var x = 42;");
        var node = tree.GetRoot().DescendantNodes().OfType<VariableDeclarationSyntax>().First();

        var result = SyntaxHelpers.GetLineNumber(node);

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task GetLineNumber_MultilineCode_ReturnsCorrectLine()
    {
        var code = """
            // Line 1
            // Line 2
            var x = 42;
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetRoot().DescendantNodes().OfType<VariableDeclarationSyntax>().First();

        var result = SyntaxHelpers.GetLineNumber(node);

        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task GetLineNumber_NestedExpression_ReturnsLineOfNode()
    {
        var code = """
            class C
            {
                void M()
                {
                    var x = 42;
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetRoot().DescendantNodes().OfType<VariableDeclarationSyntax>().First();

        var result = SyntaxHelpers.GetLineNumber(node);

        await Assert.That(result).IsEqualTo(5);
    }

    #endregion

    #region SimplifyTypeName Tests

    [Test]
    public async Task SimplifyTypeName_SystemString_ReturnsString()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.String");

        await Assert.That(result).IsEqualTo("string");
    }

    [Test]
    public async Task SimplifyTypeName_SystemInt32_ReturnsInt()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Int32");

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task SimplifyTypeName_SystemInt64_ReturnsLong()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Int64");

        await Assert.That(result).IsEqualTo("long");
    }

    [Test]
    public async Task SimplifyTypeName_SystemBoolean_ReturnsBool()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Boolean");

        await Assert.That(result).IsEqualTo("bool");
    }

    [Test]
    public async Task SimplifyTypeName_SystemObject_ReturnsObject()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Object");

        await Assert.That(result).IsEqualTo("object");
    }

    [Test]
    public async Task SimplifyTypeName_SystemVoid_ReturnsVoid()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Void");

        await Assert.That(result).IsEqualTo("void");
    }

    [Test]
    public async Task SimplifyTypeName_NullableType_StripsQuestionMark()
    {
        var result = SyntaxHelpers.SimplifyTypeName("int?");

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task SimplifyTypeName_NullableSystemType_SimplifiesAndStrips()
    {
        var result = SyntaxHelpers.SimplifyTypeName("System.Int32?");

        await Assert.That(result).IsEqualTo("int");
    }

    [Test]
    public async Task SimplifyTypeName_CustomType_ReturnsUnchanged()
    {
        var result = SyntaxHelpers.SimplifyTypeName("MyApp.UserService");

        await Assert.That(result).IsEqualTo("MyApp.UserService");
    }

    [Test]
    public async Task SimplifyTypeName_ShortKeyword_ReturnsUnchanged()
    {
        var result = SyntaxHelpers.SimplifyTypeName("string");

        await Assert.That(result).IsEqualTo("string");
    }

    #endregion

    #region Helper Methods

    private static ExpressionSyntax ParseExpression(string expression)
    {
        var tree = CSharpSyntaxTree.ParseText($"var x = {expression};");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First()
            .Initializer!
            .Value;
    }

    private static InvocationExpressionSyntax ParseInvocation(string invocation)
    {
        var tree = CSharpSyntaxTree.ParseText($"{invocation};");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();
    }

    private static TypeSyntax ParseType(string type)
    {
        var tree = CSharpSyntaxTree.ParseText($"{type} x;");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclarationSyntax>()
            .First()
            .Type;
    }

    private static TypeSyntax ParseTypeFromUnsafe(string type)
    {
        var tree = CSharpSyntaxTree.ParseText($"unsafe {{ {type} x; }}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclarationSyntax>()
            .First()
            .Type;
    }

    #endregion
}
