#r "../src/DraftSpec/bin/Debug/net10.0/DraftSpec.dll"
using static DraftSpec.Dsl;

describe("Calculator", () =>
{
    describe("add", () =>
    {
        it("returns 0 for empty input", () =>
        {
            var result = Add("");
            if (result != 0) throw new Exception($"Expected 0, got {result}");
        });

        it("returns the number for a single number", () =>
        {
            var result = Add("1");
            if (result != 1) throw new Exception($"Expected 1, got {result}");
        });

        it("returns sum of two numbers", () =>
        {
            var result = Add("1,2");
            if (result != 3) throw new Exception($"Expected 3, got {result}");
        });

        it("handles newlines as delimiters", () =>
        {
            var result = Add("1\n2,3");
            if (result != 6) throw new Exception($"Expected 6, got {result}");
        });

        it("supports custom delimiters"); // pending

        it("throws on negative numbers"); // pending
    });
});

run();

// Simple implementation for the spec
static int Add(string numbers)
{
    if (string.IsNullOrEmpty(numbers)) return 0;

    var delimiters = new[] { ',', '\n' };
    var parts = numbers.Split(delimiters);

    return parts.Sum(int.Parse);
}
