---
docType: rulebook
testType: unit
rubric: ../../../../../tests/Rubrics/Unit.md
harness: ../../../../../tests/Harness/README.md
---

## Rules

### Thread.Capture → an Active thread with only a title, every other surface empty
- **Why:** capture costs one field — parking a thought in one gesture must yield a complete record from a
  title alone: state Active, blank synthesis, no entries, no open points, identity and CreatedAt stamped.

### Thread.Capture rejects a blank title
- **Why:** the title is the one field capture costs, so it is the one field that cannot be missing — the
  invariant lives in the model, not just at the HTTP edge, so no in-process caller (a future merge, spawn,
  or test) can persist a titleless thread.

### Thread persisted through the repository → reloads whole from a fresh repository
- **Why:** a thread is one aggregate persisted whole, and a fresh repository over the same store is a host
  restart — so a thread carrying entries, open points, and a doc ref must round-trip intact, proving the
  parked idea (nested records and enums included) survives the process.

### Thread mutated via with-expressions and updated → the reloaded thread is the mutation
- **Why:** every use case is read-modify-write over an immutable record; Update followed by a fresh reload
  must return exactly the mutated shape (appended entry, changed state), proving the with-mutation and the
  store agree.
