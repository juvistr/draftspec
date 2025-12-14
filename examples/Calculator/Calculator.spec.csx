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
            expect(calc.Add("")).toBe(0);
        });

        it("returns the number for a single number", () =>
        {
            expect(calc.Add("1")).toBe(1);
        });

        it("returns sum of two numbers", () =>
        {
            expect(calc.Add("1,2")).toBe(3);
        });

        it("handles multiple numbers", () =>
        {
            expect(calc.Add("1,2,3,4,5")).toBe(15);
        });

        it("handles newlines as delimiters", () =>
        {
            expect(calc.Add("1\n2,3")).toBe(6);
        });

        it("supports custom delimiters", () =>
        {
            expect(calc.Add("//;\n1;2")).toBe(3);
        });

        it("throws on negative numbers", () =>
        {
            var ex = expect(() => calc.Add("1,-2,3,-4")).toThrow<ArgumentException>();
            expect(ex.Message).toContain("-2");
            expect(ex.Message).toContain("-4");
        });
    });
});

run();
