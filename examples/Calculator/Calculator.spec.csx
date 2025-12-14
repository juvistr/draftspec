#load "spec_helper.csx"
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

        // Unimplemented features - these will fail (TDD red phase)
        describe("large numbers", () =>
        {
            it("ignores numbers greater than 1000", () =>
            {
                expect(calc.Add("2,1001")).toBe(2);
            });

            it("includes 1000 itself", () =>
            {
                expect(calc.Add("1000,2")).toBe(1002);
            });
        });

        describe("multi-character delimiters", () =>
        {
            it("supports delimiters of any length", () =>
            {
                expect(calc.Add("//[***]\n1***2***3")).toBe(6);
            });

            it("supports multiple delimiters", () =>
            {
                expect(calc.Add("//[*][%]\n1*2%3")).toBe(6);
            });

            it("supports multiple multi-char delimiters", () =>
            {
                expect(calc.Add("//[**][%%]\n1**2%%3")).toBe(6);
            });
        });
    });

    // Pending specs - features we're planning but haven't designed yet
    describe("Subtract", () =>
    {
        it("returns 0 for empty string");
        it("returns the number for a single number");
        it("returns difference of numbers left to right");
    });

    // Skipped specs - features we're deferring
    describe("Multiply", () =>
    {
        xit("returns 1 for empty string");
        xit("returns product of numbers");
    });
});

run();
