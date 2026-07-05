---
docType: rubric
testType: arch
---

## Summary
<!-- id: b1 -->
Each Arch Rule is one dependency invariant over the loaded assemblies (ArchUnitNET) — what may reference what. Prefer deriving the assertion from one allow-graph over hand-listed denylists, so adding a band updates every rule. Enforced in `tests/Central/Arch/` (`ABox.Tests.Central`).

## Criteria

### one_invariant
<!-- id: b2 -->
States exactly one dependency or visibility invariant, not several bundled.

### named_relationship
<!-- id: b3 -->
The header names a concrete layer or component relationship, not a vague principle.

### why_justifies
<!-- id: b4 -->
The Why explains the architectural property at stake, not a restatement of the header.
