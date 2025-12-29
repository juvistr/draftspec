#load "spec_helper.csx"
using static DraftSpec.Dsl;

// ============================================
// Edge Cases Spec
// ============================================
// Demonstrates focused, skipped, and pending specs.
// These are important test cases for MTP integration.
//
// NOTE: Only run this file in isolation to test focus behavior,
// as fit() will skip specs in OTHER files too when run together.

describe("Edge Cases", () =>
{
    describe("Pending specs", () =>
    {
        // Pending spec - no body means "not yet implemented"
        it("should be implemented later");

        // Another pending spec
        it("another pending spec");
    });

    describe("Skipped specs", () =>
    {
        // xit() explicitly skips a spec - useful for temporarily disabling
        xit("is explicitly skipped", () =>
        {
            // This code never runs
            expect(false).toBeTrue();
        });

        xit("another skipped spec without body");
    });

    describe("Passing specs", () =>
    {
        it("simple assertion passes", () =>
        {
            expect(1 + 1).toBe(2);
        });

        it("null check passes", () =>
        {
            string? value = null;
            expect(value).toBeNull();
        });

        it("boolean check passes", () =>
        {
            expect(true).toBeTrue();
            expect(false).toBeFalse();
        });

        it("string contains passes", () =>
        {
            expect("hello world").toContain("world");
        });

        it("collection check passes", () =>
        {
            var items = new[] { 1, 2, 3 };
            expect(items).toContain(2);
            expect(items).toHaveCount(3);
        });
    });

    // NOTE: Focused specs (fit) are commented out because they affect
    // the entire test run. Uncomment to test focus behavior in isolation.
    //
    // describe("Focused specs", () =>
    // {
    //     fit("only this runs when focused", () =>
    //     {
    //         expect(true).toBeTrue();
    //     });
    //
    //     it("skipped due to focus above", () =>
    //     {
    //         expect(false).toBeTrue(); // Would fail if run
    //     });
    // });
});
