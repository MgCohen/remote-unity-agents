# Selector — author a block-structured doc from a dump

> **Wired:** this is the origin of the canonical `create-doc` agent
> (`.claude/agents/create-doc.md`) + `/create-doc` command. Keep them in sync;
> the agent is canonical.

Turn an ephemeral **dump** (a brain-dump or source plan) into a conformant,
block-structured instance, written to the document's **home folder** in the repo
(not a global output dir) and gated by the engine. Do NOT invent the format — read
it from the data (catalog + per-block rubrics).

## Inputs
- **The dump material** — from any of: a file path, inline pasted text, or the
  current conversation. It is scratch; the durable artifact is the block file.
- **The target doc type (optional)** — if the caller names one ("make this a
  research"), use it; otherwise infer it from the dump.
- The engine: `blocks/*.yaml`, `doctypes/*.yaml`, and the `docengine` CLI
  (`catalog` / `validate` / `outline`), run with `dotnet run --project . -- <cmd>`.

> Context caveat: "dump from the conversation" only works when you run in the
> session that holds it (a skill / main-loop run). A sub-agent starts fresh, so it
> must be handed the dump as a path or inlined text.

## Procedure
0. **Obtain the dump.** Resolve the source: read the given path, use the pasted
   text, or — when running in a session that already discussed the work — distill
   from the conversation. No file is required.
1. **Choose the doc type.** If the caller named one (e.g. "make this a research"),
   use it. Otherwise run `dotnet run --project . -- catalog` and pick the doc type
   whose `description` fits the dump — the doc-type decision matrix.
2. **Read the doc type.** `doctypes/<docType>.yaml`: its `blocks` (catalog),
   `required` set, `attrs` (front matter), and `rubric`. Follow the rubric.
3. **Pick blocks.** `dotnet run --project . -- catalog <docType>` → choose blocks whose
   `description` matches real content in the dump. Required blocks must appear.
   Include only what carries substance — no filler.
   **If content fits no block, do not force it.** A mismatch means the catalog is
   missing a block type; halt authoring, name the gap, and surface it to the caller
   (the catalog is owner-reviewed; propose, don't quietly extend). Structure never
   wins over truth.
4. **Author each block** to its own `rubric` (`blocks/<type>.yaml`):
   - Singletons → `## <Type>`. Collections → `## <Group>` then `### <title>` members.
   - Scalar attrs as `key: value` lines. Don't hand-write ids — step 6 stamps them.
   - Distill, do not transcribe. Name real files/symbols from the dump; never invent.
5. **Front matter.** Top of the file, a `---` block: `docType` plus the attrs the
   doc type declares (e.g. `status: draft`) — nothing it doesn't. Write the file to
   its home folder — where that kind of document belongs in the repo (e.g. a plan
   under `PLANS/`, an ADR under `design/adr/`), provided by the caller; there is no
   global output dir.
6. **Stamp ids.** `dotnet run --project . -- ids <path/to/doc.md> --write` gives every
   block a stable `<!-- id: … -->` handle — required, and how a client addresses a
   block. Ids freeze once written; override one only to rename it or break a collision.
7. **Gate.** `dotnet run --project . -- validate <path/to/doc.md>`; fix every
   violation; repeat until it PASSes; resolve any `!` warning it prints.
8. **Index.** `dotnet run --project . -- outline <path/to/doc.md> --write`.
9. **(Optional) grade.** `dotnet run --project . -- grading <path/to/doc.md>` emits one
   section per scope (the doc against the doctype rubric, each present block type
   against its own); the judge marks each section's lines pass/fail. Address fails.

## Discipline (mirror the doc rubric)
- The doc stands alone — no "the dump" / chat / revision language.
- One bottom Open Questions group, each with a `lean`.
- Bold marks labels, not inline emphasis.
