---
docType: rubric
testType: docs
---

## Summary
<!-- id: b1 -->
Each Docs Rule is one guarantee about the repo's structured documents, proven by shelling out to the standalone doc-engine (`tools/doc-engine`) — ADR 0015, never a reference. Enforced by a `[Rule]` fact in `tests/Central/Docs/` (`ABox.Tests.Central`) that runs `docengine` and asserts the outcome.

## Criteria

### one_guarantee
<!-- id: b2 -->
States exactly one document or catalog guarantee, not several bundled.

### engine_proven
<!-- id: b3 -->
The guarantee is one the doc-engine (`check`/`validate`/`catalog`) actually enforces.

### why_justifies
<!-- id: b4 -->
The Why gives the guarantee at stake, not a restatement of the header.
