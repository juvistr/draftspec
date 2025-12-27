// Sample spec file for MTP integration
// No run() call - MTP controls execution
using static DraftSpec.Dsl;

describe("Sample", () =>
{
    it("passes a simple assertion", () =>
    {
        expect(1 + 1).toBe(2);
    });

    it("checks string equality", () =>
    {
        expect("hello").toBe("hello");
    });

    describe("nested context", () =>
    {
        it("works in nested contexts", () =>
        {
            expect(true).toBeTrue();
        });
    });
});
