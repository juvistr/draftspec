#r "../../src/DraftSpec/bin/Debug/net10.0/DraftSpec.dll"
#r "bin/Debug/net10.0/Calculator.dll"

using static DraftSpec.Dsl;
using Calculator;

var calc = new StringCalculator();

describe("StringCalculator", () =>
{
    describe("Add", () =>
    {
        it("returns 0 for empty string", () =>
        {
            var result = calc.Add("");
            if (result != 0) throw new Exception($"Expected 0, got {result}");
        });

        it("returns the number for a single number", () =>
        {
            var result = calc.Add("1");
            if (result != 1) throw new Exception($"Expected 1, got {result}");
        });

        it("returns sum of two numbers", () =>
        {
            var result = calc.Add("1,2");
            if (result != 3) throw new Exception($"Expected 3, got {result}");
        });

        it("handles multiple numbers", () =>
        {
            var result = calc.Add("1,2,3,4,5");
            if (result != 15) throw new Exception($"Expected 15, got {result}");
        });

        it("handles newlines as delimiters", () =>
        {
            var result = calc.Add("1\n2,3");
            if (result != 6) throw new Exception($"Expected 6, got {result}");
        });

        it("supports custom delimiters", () =>
        {
            var result = calc.Add("//;\n1;2");
            if (result != 3) throw new Exception($"Expected 3, got {result}");
        });

        it("throws on negative numbers", () =>
        {
            try
            {
                calc.Add("1,-2,3,-4");
                throw new Exception("Expected ArgumentException");
            }
            catch (ArgumentException ex)
            {
                if (!ex.Message.Contains("-2") || !ex.Message.Contains("-4"))
                    throw new Exception($"Expected message to contain negatives, got: {ex.Message}");
            }
        });
    });
});

run();
