---
docType: rulebook
testType: unit
rubric: ../../../../../tests/Rubrics/Unit.md
harness: ../../../../../tests/Harness/README.md
---

## Rules

### Thread.Capture → an Active thread with only a title, every other surface empty
<!-- id: b1 -->
- **Why:** capture costs one field — parking a thought in one gesture must yield a complete record from a
  title alone: state Active, blank synthesis, no entries, no open points, identity and CreatedAt stamped.

### Thread.Capture rejects a blank title
<!-- id: b2 -->
- **Why:** the title is the one field capture costs, so it is the one field that cannot be missing — the
  invariant lives in the model, not just at the HTTP edge, so no in-process caller (a future merge, spawn,
  or test) can persist a titleless thread.

### Thread persisted through the repository → reloads whole from a fresh repository
<!-- id: b3 -->
- **Why:** a thread is one aggregate persisted whole, and a fresh repository over the same store is a host
  restart — so a thread carrying entries, open points, and an entry link (artifact or session) must
  round-trip intact, proving the parked idea (polymorphic records and enums included) survives the process.

### Thread mutated via with-expressions and updated → the reloaded thread is the mutation
<!-- id: b4 -->
- **Why:** every use case is read-modify-write over an immutable record; Update followed by a fresh reload
  must return exactly the mutated shape (appended entry, changed state), proving the with-mutation and the
  store agree.

### ThreadFiles.Save → the file written once; a taken name is refused forever
<!-- id: b5 -->
- **Why:** one rule replaces an integrity subsystem — a name, once written, is immutable — so every DocRef
  ever appended to a journal stays permanently valid; the refusal must also leave the original content
  untouched, and a failed upload must never claim the name.

### ThreadFiles content round-trips by the DocRef it minted
<!-- id: b6 -->
- **Why:** the input decides the name — the caller's relative path is echoed back as the DocRef, and that
  DocRef is the one handle receipts carry, so bytes saved under it must come back by it and an absent path
  must answer null, not fabricate.

### ThreadFiles refuses paths that escape the thread's folder
<!-- id: b7 -->
- **Why:** an artifact has exactly one owner and lifecycle travels with the folder, so the port — the one
  door every caller crosses, HTTP or the future session wrap-up — must canonically resolve each path and
  refuse anything absolute, blank, or traversing out, leaving no trace behind.

### ThreadFiles.List → every file as a folder-prefixed relative path
<!-- id: b8 -->
- **Why:** the drop zone is browsable by construction — listing returns paths relative to the thread's
  folder so subfolders (notes, artifacts, whatever comes) separate naturally, and a thread with no
  folder yet lists empty rather than erroring.

### ThreadFiles refuses to read or write through a symlink out of the folder
<!-- id: b9 -->
- **Why:** lifecycle travels with the folder, so the folder must actually contain everything reachable
  through it — a planted symlink pointing out is refused on read, on write, and skipped in listings, and
  a folder-shaped name (trailing separator, no filename) is a caller error, not a 500.

### ThreadFiles.Save → an empty body never claims a name
<!-- id: b10 -->
- **Why:** a name once written is immutable, so a truncated or bodyless upload must not permanently burn a
  DocRef over zero bytes — an empty save is refused and the name stays free for a real one.

### ThreadFiles reserves the .tmp suffix for in-flight uploads
<!-- id: b11 -->
- **Why:** uploads stage beside their target before moving into place, so the staging suffix cannot be a
  caller's name and staged or crash-orphaned files never appear in listings — the browsable drop zone
  shows only what was deliberately saved.
