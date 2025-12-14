# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DraftSpec is an early-stage project to build an expressive spec-first testing framework for .NET 10, inspired by RSpec/Jasmine. The goal is to fill the gap left by abandoned frameworks like NSpec by leveraging modern C# 14 features.

## Key C# 14/.NET 10 Features Under Consideration

- **Extension members**: Enable property-based assertion syntax (not just methods)
- **CallerArgumentExpression**: Capture source expressions for meaningful assertion failures
- **Source generators with interceptors**: Reflection-free test discovery for Native AOT
- **params collections**: Zero-allocation variadic methods via `params Span<T>`

## Design Goals

- RSpec-style nesting with `describe/context/it` hierarchy
- First-class pending specs (document intent before implementation)
- Focus (`fit`/`fdescribe`) and skip (`xit`/`xdescribe`) mechanisms
- Let-style memoization
- AOT compatibility via source generators
- Rich CLI output (Spectre.Console)

## Related Research

See `docs/RESEARCH.md` for detailed analysis of existing frameworks, C# 14 features, and architectural options. Note that specific syntax patterns in that document are suggestions to evaluate, not decisions.
