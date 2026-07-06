---
docType: rubric
testType: structure
---

## Summary
<!-- id: b1 -->
Each Structure Rule is one source-placement invariant over `src/` and `tests/`, read straight from disk so it holds before code compiles. Enforced in `tests/Central/Structure/` (`ABox.Tests.Central`).

## Criteria

### one_placement
<!-- id: b2 -->
States exactly one source-placement invariant, not several bundled.

### on_disk
<!-- id: b3 -->
The rule is decidable from the file tree before compile, not a runtime or reference-graph property.

### why_justifies
<!-- id: b4 -->
The Why names the blind spot it closes on disk, not a restatement of the header.
