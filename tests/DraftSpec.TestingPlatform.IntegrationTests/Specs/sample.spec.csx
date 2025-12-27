#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

describe("Sample", () =>
{
    it("passes", () =>
    {
        expect(1 + 1).toBe(2);
    });
});

// Note: run() is not called - MTP will control execution
