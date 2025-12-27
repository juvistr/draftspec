// Root-level sample spec file for MTP integration
// No run() call - MTP controls execution
using static DraftSpec.Dsl;

describe("Root Sample", () =>
{
    it("demonstrates root-level specs", () =>
    {
        expect(42).toBe(42);
    });
});
