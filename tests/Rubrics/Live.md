---
docType: rubric
testType: live
---

## Summary
<!-- id: b1 -->
Each Live Rule is one real-CLI guarantee — a flow or agent against the real `claude`/`codex` CLI and subscription, gated behind `[LiveFact]` / `RUN_LIVE=1`. Enforced in each feature's co-located `src/<…>/<Owner>/Tests/Live/` (`ABox.<Owner>.Tests`).

## Criteria

### one_effect
<!-- id: b2 -->
States exactly one real-world effect of the live run, not several bundled.

### needs_live
<!-- id: b3 -->
The effect genuinely requires the real CLI/subscription — a scripted provider could not prove it.

### why_justifies
<!-- id: b4 -->
The Why names the live behaviour no scripted provider can prove, not a restatement of the header.
