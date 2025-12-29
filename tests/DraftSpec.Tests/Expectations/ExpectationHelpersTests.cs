using DraftSpec.Expectations;

namespace DraftSpec.Tests.Expectations;

public class ExpectationHelpersTests
{
    [Test]
    public async Task Format_Null_ReturnsNullString()
    {
        var result = ExpectationHelpers.Format(null);

        await Assert.That(result).IsEqualTo("null");
    }

    [Test]
    public async Task Format_String_ReturnsQuotedString()
    {
        var result = ExpectationHelpers.Format("hello");

        await Assert.That(result).IsEqualTo("\"hello\"");
    }

    [Test]
    public async Task Format_Integer_ReturnsToString()
    {
        var result = ExpectationHelpers.Format(42);

        await Assert.That(result).IsEqualTo("42");
    }

    [Test]
    public async Task Format_Object_ReturnsToString()
    {
        var obj = new TestObject { Value = "test" };

        var result = ExpectationHelpers.Format(obj);

        await Assert.That(result).IsEqualTo("TestObject: test");
    }

    private sealed class TestObject
    {
        public string Value { get; init; } = "";
        public override string ToString() => $"TestObject: {Value}";
    }
}
