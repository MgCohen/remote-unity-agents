---
docType: rulebook
testType: wire
rubric: ../../../../../tests/Rubrics/Wire.md
harness: ../../../../../tests/Harness/README.md
---

## Rules

### POST /threads → a created Active thread from a title alone, rejecting a blank title
<!-- id: b1 -->
- **Why:** capture costs one field — one gesture with a title must mint the whole record (Active, empty
  surfaces, Location pointing at the new thread), and a blank title is the one input that cannot become a
  thread, so it is refused at the wire.

### GET /threads/{id} → the thread with synthesis, journal, and margin, or 404 when absent
<!-- id: b2 -->
- **Why:** rehydration is the memory feature — picking a thread back up must return where-we-are, what
  happened, and what's unresolved in one read, and an unknown id must say so rather than fabricate.

### GET /threads → Active threads by default; a state query selects any single defined state, rejecting unknown values
<!-- id: b3 -->
- **Why:** the working set is what's alive — archived and completed threads must not crowd the default
  view yet stay one query away (the shelf is browsable, nothing is deleted), and a value outside the
  three defined states is a caller error, not an empty filter.

### POST /threads/{id}/entries → the entry appended server-stamped, rejecting a blank summary or a missing/unknown author
<!-- id: b4 -->
- **Why:** the journal is permanent — an entry is a jot (no doc) or a session receipt (summary + doc ref)
  stamped with a server clock so ordering is trustworthy, and because nothing appended can ever be
  corrected, a summary-less, author-less, or unknown-author entry must never get in.

### Entries have no update or delete surface
<!-- id: b5 -->
- **Why:** the journal is append-only by construction — history stays trustworthy because the wire offers
  no verb to rewrite or erase what happened, so any such request is refused (unrouted or method-not-allowed).

### PUT /threads/{id}/synthesis → the synthesis replaced wholesale — blank included — and SynthesizedAt stamped
<!-- id: b6 -->
- **Why:** synthesis is the one rewritable surface, human-owned — any PUT is a deliberate rewrite,
  including clearing it, and the stamp makes freshness visible so a stale synthesis is detectable at
  pickup.

### POST /threads/{id}/openpoints → a point minted with its removal id, rejecting blank text
<!-- id: b7 -->
- **Why:** open points are parked in one gesture — the server mints the id (the only unambiguous removal
  handle under duplicate texts) and the timestamp (staleness stays visible), and empty text is not a
  point.

### DELETE /threads/{id}/openpoints/{pointId} → the margin forgets, idempotently
<!-- id: b8 -->
- **Why:** the margin holds no history — a resolved point vanishes without trace (its resolution lives in
  the journal), and re-deleting an already-gone point is a no-op success because DELETE must stay
  idempotent.

### PUT /threads/{id}/state → the relabeled thread, every defined transition legal, rejecting unknown values
<!-- id: b9 -->
- **Why:** state is a label, not a state machine — any of the three defined states is reachable from any
  other (archive is one call from revival by design), while an undefined value would strand the thread
  outside every view, permanently mislabeled.

### PUT /threads/{id}/files/{path} → the file created at the caller-named path, rejecting bad names, refusing taken names
<!-- id: b10 -->
- **Why:** the input decides the name — the raw body lands at the relative path in the route and that path
  is the DocRef receipts will carry — so an escaping path, a folder-shaped name, or an empty body is a
  caller error (400), a taken name is a conflict the immutability rule turns loud (409), and a thread that
  doesn't exist takes no files (404).

### GET /threads/{id}/files/{path} → the bytes as saved regardless of thread state, or 404 when absent
<!-- id: b11 -->
- **Why:** a DocRef is permanently valid — archiving leaves the folder untouched, so a receipt's doc must
  fetch byte-for-byte after the thread is shelved and revived, and an unknown path must say so rather
  than fabricate.

### GET /threads/{id}/files → every saved file as a folder-prefixed path
<!-- id: b12 -->
- **Why:** the drop zone is browsable — one read shows what the thread holds, subfolders separated by
  their path prefixes, so a session can survey the artifacts before diving into any.
