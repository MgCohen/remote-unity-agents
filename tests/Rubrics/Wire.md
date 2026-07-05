---
docType: rubric
testType: wire
---

## Summary
<!-- id: summary -->
Each Wire Rule is one endpoint contract, proven with a real `HttpClient` against the Host over `WebApplicationFactory<Program>`, backed by a CLI-free flow. One Rule per endpoint behaviour, enforced by a `[Rule]` fact in each feature's co-located `src/<…>/<Owner>/Tests/Wire/` (`ABox.<Owner>.Tests`).

## Criteria

### one_contract
<!-- id: one-contract -->
Exactly one endpoint contract (method + route → result), not several bundled.

### observable
<!-- id: observable -->
Asserts observable wire behaviour (status, body shape, SSE stream), not an implementation detail.

### why_justifies
<!-- id: why-justifies -->
The Why gives the guarantee behind the endpoint, not a restatement of the header.
