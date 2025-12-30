---
name: Bug Report
about: Report a bug in DraftSpec
title: ''
labels: bug
assignees: ''
---

## Description

A clear description of what the bug is.

## Environment

- DraftSpec version:
- .NET version:
- OS:

## Steps to Reproduce

1. Create a spec file with...
2. Run `draftspec run ...`
3. See error

## Spec File (if applicable)

```csharp
#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

describe("Example", () =>
{
    it("fails unexpectedly", () =>
    {
        // minimal reproduction
    });
});
```

## Expected Behavior

What you expected to happen.

## Actual Behavior

What actually happened. Include error output if available.

## Additional Context

Any other context about the problem.
