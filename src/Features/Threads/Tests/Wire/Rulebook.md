---
docType: rulebook
testType: wire
rubric: ../../../../../tests/Rubrics/Wire.md
harness: ../../../../../tests/Harness/README.md
---

## Rules

### POST /threads → a created Active thread from a title alone, rejecting a blank title
- **Why:** capture costs one field — one gesture with a title must mint the whole record (Active, empty
  surfaces, Location pointing at the new thread), and a blank title is the one input that cannot become a
  thread, so it is refused at the wire.

### GET /threads/{id} → the thread with all four sub-surfaces, or 404 when absent
- **Why:** rehydration is the memory feature — picking a thread back up must return synthesis, journal,
  and margin in one read, and an unknown id must say so rather than fabricate.

### GET /threads → Active threads by default; a state query selects any single state
- **Why:** the working set is what's alive — archived and completed threads must not crowd the default
  view, yet stay one query away (the shelf is browsable, nothing is deleted).

### POST /threads/{id}/entries → the entry appended server-stamped, rejecting a blank summary
- **Why:** the journal holds deliberate annotations — a jot (no doc) or a session receipt (summary +
  doc ref) — stamped with a server clock so ordering is trustworthy, and an empty summary is not an
  annotation.

### Entries have no update or delete surface
- **Why:** the journal is append-only by construction — history stays trustworthy because the wire offers
  no verb to rewrite or erase what happened, so any such request is refused (unrouted or method-not-allowed).

### PUT /threads/{id}/synthesis → the synthesis replaced and SynthesizedAt stamped
- **Why:** synthesis is the one rewritable surface — "where we are" must be replaceable wholesale at any
  time, with its freshness visible so a stale synthesis is detectable at pickup.

### POST + DELETE open points → the margin grows by minted id and forgets on removal, idempotently
- **Why:** open points are a disposable margin — added in one gesture with a server-minted id (the only
  unambiguous removal handle), gone without trace when resolved, and re-deleting an already-gone point is
  a no-op success because DELETE must stay idempotent.

### PUT /threads/{id}/state → every transition legal, and the default List reflects it
- **Why:** state is a label, not a state machine — archive must be one call from revival (shelving is
  reversible by design), and the label's whole job is scoping the default working set.
