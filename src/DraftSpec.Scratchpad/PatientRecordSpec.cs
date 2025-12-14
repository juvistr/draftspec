using DraftSpec;

namespace DraftSpec.Scratchpad;

public class PatientRecordSpec : Spec
{
    public PatientRecordSpec()
    {
        describe("Patient record", () =>
        {
            context("when created with valid data", () =>
            {
                it("stores the patient name", () =>
                {
                    // TODO: actual assertion
                    var name = "John Smith";
                    if (name != "John Smith") throw new Exception("Name mismatch");
                });

                it("validates NHS number format"); // pending - not yet implemented

                it("calculates age from date of birth", () =>
                {
                    var dob = new DateTime(1990, 5, 15);
                    var age = DateTime.Today.Year - dob.Year;
                    if (age < 30) throw new Exception($"Expected age >= 30, got {age}");
                });
            });

            context("when data is invalid", () =>
            {
                xit("rejects empty names", () =>
                {
                    // skipped for now
                });

                it("rejects future dates of birth", () =>
                {
                    // This will fail to demonstrate failure output
                    throw new InvalidOperationException("Date validation not implemented");
                });
            });
        });
    }
}
