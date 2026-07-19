# Migrating Governance to a New Repo

**What this is.** A self-contained guide for lifting **governance** — and the four
subsystems it manages — out of `abox-server` and into a fresh repository being built
from scratch.

**Who it's for.** Someone standing up the new repo who has *not* seen `abox-server`
before. No prior context assumed. Read Part 1 to understand *what* governance is and
*why* each subsystem exists; follow Part 2 to actually do it.

**Ground rule.** We are porting **infrastructure**, not product behavior. The machinery
(the policy engine, the parity harness, the doc validator, the shell guards) travels
cleanly. The *content* those tools enforce — the specific architecture rules, folder
names, doctypes, fixtures — describes the old product and must be **re-authored**, not
copied. This keeps the port on the right side of the "rebuild, don't reuse" line.

Throughout, `<NEW>` is a placeholder for your new product's namespace prefix (what
`ABox` becomes). A full substitution table is in [§4](#4--rename--config-reference).

---

## Table of contents

- [Part 1 — Context: the model](#part-1--context-the-model)
  - [The thesis: structure over prose](#the-thesis-structure-over-prose)
  - [Governance: the main system](#governance-the-main-system)
  - [Subsystem 1 — Test Harness (Rulebook discipline)](#subsystem-1--test-harness-rulebook-discipline)
  - [Subsystem 2 — Doc Engine](#subsystem-2--doc-engine)
  - [Subsystem 3 — CODEOWNERS / CI](#subsystem-3--codeowners--ci)
  - [Subsystem 4 — Hooks](#subsystem-4--hooks)
  - [The one seam: declarations governed, proof distributed](#the-one-seam-declarations-governed-proof-distributed)
- [Part 2 — The migration](#part-2--the-migration)
  - [Step 0 — Decisions before you touch a file](#step-0--decisions-before-you-touch-a-file)
  - [Step 1 — Lay the build & test seam](#step-1--lay-the-build--test-seam)
  - [Step 2 — Governance core (policy + checker + generator)](#step-2--governance-core-policy--checker--generator)
  - [Step 3 — Test Harness](#step-3--test-harness)
  - [Step 4 — Central tests (re-author the content)](#step-4--central-tests-re-author-the-content)
  - [Step 5 — Doc Engine](#step-5--doc-engine)
  - [Step 6 — CODEOWNERS / CI + Hooks + the GitHub ruleset](#step-6--codeowners--ci--hooks--the-github-ruleset)
- [§3 — Verification: done-when checklist](#3--verification-done-when-checklist)
- [§4 — Rename & config reference](#4--rename--config-reference)
- [§5 — File inventory: port / rename / re-author / drop](#5-file-inventory-port--rename--re-author--drop)

---

# Part 1 — Context: the model

## The thesis: structure over prose

One idea sits under everything here — **the agent-first wager**:

> Rules are **structure the machine checks**, not prose humans (or agents) are trusted
> to follow. A guarantee encoded as a project reference, a parity check, or a schema
> fires in the agent's *own* build loop and is **tamper-evident** — evading it is a
> visible diff a reviewer can gate, not a silently suppressed comment.

Everything below is a consequence of that wager. Governance is the system that lives it.

## Governance: the main system

**Governance is the main system.** Its core is tiny: **one policy file**
(`protected-paths`) that declares what's load-bearing, plus **one shared checker** and a
**CODEOWNERS generator**. On top of that core, governance **manages four subsystems**:

![Governance is the main system managing four subsystems](assets/governance-model.svg)

| Subsystem | What it governs | One line |
|---|---|---|
| **Test Harness** | test guarantees | Declare a guarantee as a **Rule**; prove it with a co-located `[Rule]` test; parity keeps them in lockstep. |
| **Doc Engine** | document structure | Structure-over-prose for docs: kinds → doctypes → block instances → a validator. Defines the shape of a Rulebook itself. |
| **CODEOWNERS / CI** | what merges | Required code-owner review is the **merge gate of record**; CI adds advisory labels + alerts + the identity split. |
| **Hooks** | local edits | `pre-commit` / `pre-push` catch protected-path edits before they're even pushed. |

**Why one system, not four loose tools.** They all read the **same policy**
(`protected-paths`) and share **one checker** (`protected-paths-check.sh`). Change the
policy once and every subsystem stays in sync. That single source of truth is what makes
governance a system rather than a pile of scripts.

---

## Subsystem 1 — Test Harness (Rulebook discipline)

**What it is.** The subsystem that makes test guarantees structural. Each *kind* of test
owns a **Rulebook** — a file whose `### ` headers are plain-English **Rules**. Every Rule
must have matching test code (`[Rule("<header>")]`), and the engine fails the build if the
two ever drift apart.

**What it does.**

- **Parity:** every `### ` header has ≥1 `[Rule]` test; every `[Rule]` cites a real
  header. No orphans on either side.
- **Taxonomy:** every test lives in a **registered type** (Arch, Structure, Unit, E2E,
  Wire, Live, Docs). A stray folder fails the build.
- **Co-location:** a feature's tests live *with* the feature; the engine discovers them by
  location, so adding a feature needs no central wiring.

**Why we want it.** The suite becomes self-documenting and self-policing — you read the
Rulebook to know what's guaranteed, and you can't drop a guarantee by accident because
parity goes red. It's a **ratchet**: easy to tighten, deliberately hard to loosen.

![Test Harness — the Rulebook ratchet](assets/harness-flow.svg)

| File / folder | Role |
|---|---|
| `tests/Harness/` | Shared base: `[Rule]`/`[LiveFact]` attributes, `Report`, `RepoTree` locator. |
| `tests/Harness/Tests/` | The engine + its own tests: `ParityGuard`, `TestTypes`, `Suites`. |
| `tests/Central/` | The ownerless structural suites: `Arch`, `Structure`, `Docs`. |
| `tests/Rubrics/` | Per-type criteria a Rulebook's Rules are graded against. |
| `dirs.proj` | Test-discovery seam — globs every test project so `dotnet test dirs.proj` runs them all. |

---

## Subsystem 2 — Doc Engine

**What it is.** Structure-over-prose applied to **documents**. A standalone .NET tool
(`ABox.DocEngine`, the `docengine` CLI) with a data-defined catalog: a meta-schema →
**kinds** → **doctypes** → **block instances**, plus a validator. It defines the *shape*
of a document and validates real documents against it — in place, wherever they live.

**Why it belongs to governance.** The Rulebook and the rubric are themselves **doctypes**
(ADR 0015). So Doc Engine is what gives the Test Harness its declared structure — and it
also validates ADRs and plans the same way. It is the schema floor beneath every
structured document in the repo, not a bolt-on to one test type.

**The loop.**

```
dump (ephemeral)  ──distill──►  instance.md (blocks)  ──validate──►  pass / fail
  a brain-dump                 a Rulebook / ADR / plan          docengine validate
   scratch, discarded          in its home folder               against doctypes/*.yaml
```

**The three-layer Rulebook** (why Doc Engine and the Test Harness interlock):

| Layer | What it is | Home |
|---|---|---|
| **Doctype** (`rulebook`, `rubric`) | the schema — what *any* Rulebook/rubric must look like | central — `tools/doc-engine/doctypes/` |
| **`<type>.md`** | the per-type criteria ("what a Unit test is") | central — `tests/Rubrics/` |
| **`Rulebook.md`** | this feature's guarantees | co-located with the feature |

| File / folder | Role |
|---|---|
| `tools/doc-engine/_schema/` | The meta-schema — the floor the whole catalog conforms to. |
| `tools/doc-engine/kinds/`, `blocks/`, `doctypes/` | The data-defined catalog (incl. the `rulebook` + `rubric` doctypes). |
| `tools/doc-engine/*.cs` | The CLI: `SchemaChecker`, `InstanceParser`, `DocValidator`, `Catalog`, `Outline`. |

---

## Subsystem 3 — CODEOWNERS / CI

**What it is.** The **server-side** half of governance — the part that decides what can
merge, and makes protected-path changes visible in CI.

**What it does.**

- **CODEOWNERS** — generated from `protected-paths`; required code-owner review is the
  **merge gate of record** (a CI check can't distinguish an *approved* change from an
  unreviewed one; a required review can).
- **`policy-guard` CI job** — verifies CODEOWNERS is in sync (this step can fail the
  build) and *advisorily* annotates + tier-labels PRs that touch protected paths.
- **Identity split** — a non-admin bot authors PRs; the owner approves. `identity-check.sh`
  proves commits are the bot, never the owner.
- **Notifier** (optional) — a push alert when a `critical`-tier path changes.

![One policy drives every enforcer](assets/governance-flow.svg)

| File | Role |
|---|---|
| `.github/CODEOWNERS` | Generated owner map — the merge gate. Never hand-edit; regenerate. |
| `.github/workflows/ci.yml` (`policy-guard`) | CODEOWNERS-sync check + advisory annotations & PR tier labels. |
| `governance/identity-check.sh` | Proves commits are authored by the bot, never the owner. |
| `governance/notify*` | Optional critical-path push alerts (via Apprise/ntfy). |

---

## Subsystem 4 — Hooks

**What it is.** The **local** half — fast git hooks that catch a protected-path edit
before it's ever pushed. Convenience, not the guarantee: they're opt-in per clone and
bypassable (`--no-verify`). The real gate is CODEOWNERS.

| File | Role |
|---|---|
| `.githooks/pre-commit` | Blocks a commit that touches a protected path. |
| `.githooks/pre-push` | Blocks a push whose range touches a protected path. |
| `.gitattributes` | Pins the enforcer files to LF so a CRLF checkout can't break the shell guards. |

Enable per clone: `git config core.hooksPath .githooks`.

---

## The one seam: declarations governed, proof distributed

The single most important relationship to carry across:

> **Declarations are governed; proof is distributed.** Every Rulebook, rubric, and
> doctype is a **protected path** — central, owner-reviewed. The Rules *live inside
> governance*. The only thing that lives **outside** the wall is the specific `[Rule]`
> test code, co-located with the feature it protects.

```
governance/protected-paths ──guards──► Rulebooks · rubrics · doctypes · CI · build config
   (the declarations)                        │
tests/**/*.cs [Rule] facts ◄── prove ────────┘   (the proof — co-located, NOT protected)
tests/Central/Docs ──shells out to──► tools/doc-engine   (Doc Engine validates the docs)
```

That's why renaming or dropping a subsystem means the **policy rows** must follow: the
declarations and their guard move together.

---

# Part 2 — The migration

One unified path. Do the steps in order — each leaves the repo in a buildable state.

> **Legend for every step:** the four cues **Goal**, what to **Copy**, what to **Edit**,
> and how to **Verify**.

## Step 0 — Decisions before you touch a file

Settle these first; they change *what* you port. Every subsystem is core to the model —
the only real question is **sequencing** (which land in v1 vs later).

| Decision | Options | Recommended |
|---|---|---|
| **Doc Engine** | Port the full `tools/doc-engine` now **or** defer it (the Test Harness ratchet still works via parity; you just lose in-place doc validation + the `Docs` type until it lands). | **Defer past the first green build**, then port — it's the largest single piece, so don't let it block the walking skeleton. It is *not* dropped: it's a core subsystem. |
| **`Docs` test type** | Register it now (needs Doc Engine) **or** register it when Doc Engine lands. | Land it **with** Doc Engine (Step 5). |
| **Critical-path notifier** | Wire `notify*` (needs an ntfy/Apprise channel + secret) **or** defer. | **Defer.** Keep the CODEOWNERS gate + hooks; add alerts later. |
| **`.claude` PreToolUse guard** | Wire a Claude hook that calls the checker **or** rely on hooks + CI. | **Wire it** if you use Claude Code — cheap local backstop. |
| **Live tests** | Keep the gated real-CLI `Live` type **or** drop until you have a CLI. | Keep the *type*, no cases yet — it stays skipped without `RUN_LIVE=1`. |

**Pick your names now** (used everywhere downstream):

| Placeholder | Meaning | Example |
|---|---|---|
| `<NEW>` | Product namespace prefix (replaces `ABox`) | `Acme` |
| `<owner>` | GitHub handle that reviews protected paths | `@your-handle` |
| `<bot>` | Machine account that authors agent commits | `Acme-Agent` |

> **Deferring Doc Engine** means: leave `"Docs"` out of `TestTypes.Registered`, and drop
> the four `tools/doc-engine/{doctypes,blocks,kinds,_schema}/**` rows from `protected-paths`
> for now (the `tests/Rubrics/**` glob stays — it still covers your remaining types). Add
> them back in Step 5.

## Step 1 — Lay the build & test seam

**Goal.** A new repo that builds and runs an (empty) test pass.

**Copy**, renaming as you go:

- `<NEW>.slnx` (was `ABox.slnx`) — the solution *and* the repo-root marker.
- `Directory.Build.props`, `.editorconfig`, `.gitattributes`, `.gitignore`
- `dirs.proj`, `tests/TestProject.props`

**Edit.**

- In `Directory.Build.props`, change the namespace derivation prefix `ABox.` → `<NEW>.`
- In `dirs.proj` and the props, confirm the glob paths (`src/**`, `tests/**`, `tools/**`)
  match your intended layout.

**Verify.** `dotnet build <NEW>.slnx` succeeds (nothing to compile yet, but config is valid).

## Step 2 — Governance core (policy + checker + generator)

**Goal.** The spine every subsystem reads: one policy, one checker, one generator.

**Copy** `governance/protected-paths`, `protected-paths-check.sh`, and
`generate-codeowners.sh`.

**Edit.**

- `protected-paths` — owner `@MgCohen` → `<owner>`; prune rows for anything you haven't
  built yet (you'll add subsystem rows back as you land each one); confirm each glob
  matches your layout.
- Optionally rename the `ABOX_ALLOW_PROTECTED` override env var → `<NEW>_ALLOW_PROTECTED`
  (it lives in the checker and README; the hooks just call the checker).

**Verify.** `./governance/generate-codeowners.sh` writes a `.github/CODEOWNERS` and a
re-run produces no diff; `printf 'CLAUDE.md\n' | ./governance/protected-paths-check.sh`
exits non-zero (a protected path is caught).

## Step 3 — Test Harness

**Goal.** The parity engine builds and can find the repo root.

**Copy** `tests/Harness/**` and `tests/Rubrics/**`.

**Edit** — the **hardcoded couplings** (there are only a few):

| File | Change |
|---|---|
| `tests/Harness/RepoTree.cs` | `Marker = "ABox.slnx"` → `"<NEW>.slnx"` |
| `tests/Harness/TestAssemblies.cs` | `Prefix`, `SharedPrefix` (`"ABox."`, `"ABox.Tests."`) |
| `tests/Harness/Tests/TestTypes.cs` | Namespace + the `Registered` set (leave out `Docs` until Step 5) |
| All `.cs` under `tests/Harness/` | `namespace ABox.Tests.*` → `<NEW>.Tests.*` (IDE0130 forces this) |
| The three csprojs + `TestProject.props` | `AssemblyName` / `RootNamespace` / `<Using Include="ABox.Tests.Harness"/>` |

**Verify.** `dotnet build tests/Harness/Tests/<NEW>.Tests.Harness.Tests.csproj` succeeds and
`RepoTree` finds your new marker (no "could not locate repo root" throw).

## Step 4 — Central tests (re-author the content)

**Goal.** `Arch` and `Structure` suites that describe **your** architecture, green under parity.

**Copy** `tests/Central/` **shape** — the per-type folder layout (`<Type>/Rulebook.md`, the
`.cs`, `<Type>/Support/`) and `SuiteAnchor`.

> ⚠️ **This is the re-author step, not a copy step.** The *Rules themselves* encode
> `abox-server`'s layer graph and folder names. Keep the structure; rewrite the content:

- **`Arch/Support`** — replace the layer allow-graph with your architecture's.
- **`Structure/Support`** — replace the "home folders" set with your `src/` layout.
- **`Rulebook.md` files** — rewrite each `### ` Rule to state *your* invariants, then write
  the matching `[Rule("…")]` fact. Start with a handful; add liberally later.

**Edit.** csproj `AssemblyName`/`RootNamespace` → `<NEW>.Tests.Central`; the production
`ProjectReference` glob `src\**\ABox.*.csproj` → `<NEW>.*.csproj`.

**Verify.** `dotnet test dirs.proj` runs and is green; parity passes (every Rule has a fact,
every fact cites a Rule).

## Step 5 — Doc Engine

**Goal.** Structured documents validated in place; the `Docs` test type live.

**Copy** `tools/doc-engine/**` (the full tool — CLI `.cs`, `_schema/`, `kinds/`, `blocks/`,
`doctypes/`, `scripts/`, `howto/`).

**Edit.**

- Assembly/namespace `ABox.DocEngine` → `<NEW>.DocEngine`.
- Re-author the **catalog content** for your docs: keep the `rulebook` / `rubric` doctypes
  (they're generic), but your ADR/plan doctypes and any blocks describe old-product docs —
  adapt them.
- Re-add to `protected-paths`: the four `tools/doc-engine/{doctypes,blocks,kinds,_schema}/**`
  rows and `tools/**/Tests/**/Rulebook.md`.
- Register `"Docs"` in `TestTypes.Registered` and restore `tests/Central/Docs/`.

**Verify.** `docengine check` passes (the catalog conforms to the meta-schema);
`docengine validate <a Rulebook>` passes; `dotnet test dirs.proj` runs the `Docs` type green.

## Step 6 — CODEOWNERS / CI + Hooks + the GitHub ruleset

**Goal.** The enforcers wired, and the server-side guarantees the files *assume*.

**Copy** `.githooks/**`, the `.github/workflows/ci.yml` `policy-guard` job, and (if kept)
`governance/notify*`.

**Edit.**

| File | Change |
|---|---|
| `governance/identity-check.sh` | `BOT_NAME`/`BOT_EMAIL` → `<bot>`; `OWNER_NAME`/`OWNER_EMAIL_MARK` → your owner. |
| `.github/workflows/ci.yml` | Required-check names (`build-test (ubuntu/windows-latest)`), `dotnet-version`, and (if kept) the `NTFY_TOPIC` secret. |

**Configure on GitHub** (repo settings, not code):

- [ ] **Branch ruleset on `main`** — require a PR before merge; require the CI checks
      (`build-test (ubuntu-latest)` + `windows-latest`); require **code-owner review**;
      block force-push; empty bypass list.
- [ ] **Identity split** — create the `<bot>` machine account (non-admin), have it author
      PRs, and require **1 approval + last-push approval** so the bot can't self-merge a
      protected change.
- [ ] **Secrets** — add `NTFY_TOPIC` *only if* you kept the notifier (low-value string, safe
      in PR CI; never put a real credential in the PR-triggered step).

**Verify.** Touch a protected path on a branch → the pre-commit hook blocks it; a PR from the
bot touching a protected path is blocked until `<owner>` approves.

---

# §3 — Verification: done-when checklist

- [ ] `dotnet build <NEW>.slnx` — warning-free.
- [ ] `dotnet test dirs.proj` — green; parity + taxonomy guards pass.
- [ ] `RepoTree` finds the new `<NEW>.slnx` marker.
- [ ] `generate-codeowners.sh` re-run produces **no** diff.
- [ ] Editing a protected path is blocked by the git hook and flagged by CI.
- [ ] `docengine check` + `docengine validate <doc>` pass (if Doc Engine ported).
- [ ] `identity-check.sh` passes as `<bot>`, fails if it detects the owner identity.
- [ ] Branch ruleset blocks an unreviewed protected-path merge to `main`.
- [ ] No stray `ABox` / `@MgCohen` / `ABox-Agent` string remains
      (`grep -ri 'abox\|mgcohen' .` comes back clean except intentional history).

# §4 — Rename & config reference

**Mechanical renames** (find/replace across the ported files):

| From | To | Where it appears |
|---|---|---|
| `ABox.slnx` | `<NEW>.slnx` | solution file + the `RepoTree.cs` root marker |
| `ABox.` (namespace prefix) | `<NEW>.` | all `.cs`, `Directory.Build.props`, csprojs |
| `ABox.Tests.Central/.Harness` | `<NEW>.Tests.Central/.Harness` | csprojs, `SuiteAnchor`, usings |
| `ABox.DocEngine` | `<NEW>.DocEngine` | doc-engine csproj + namespaces |
| `ABox.*.csproj` (glob) | `<NEW>.*.csproj` | `tests/Central` production ProjectReference |

**Real config edits** (judgment, not find/replace):

| Setting | File | Change |
|---|---|---|
| Owner handle | `governance/protected-paths` | `@MgCohen` → `<owner>` (then regenerate) |
| Bot identity | `governance/identity-check.sh` | `BOT_*`, `OWNER_*` |
| Registered types | `tests/Harness/Tests/TestTypes.cs` | add `Docs` when Doc Engine lands |
| Required checks | `.github/workflows/ci.yml` + ruleset | must match the job names exactly |
| Protected paths | `governance/protected-paths` | add each subsystem's rows as you land it |

# §5 — File inventory: port / rename / re-author / drop

| Path | Action | Note |
|---|---|---|
| `governance/protected-paths-check.sh`, `generate-codeowners.sh` | **Port as-is** | Generic; zero product coupling. |
| `.githooks/**`, `.gitattributes` | **Port as-is** | Path literals only. |
| `tests/Harness/**` | **Rename** | Namespace + marker + prefixes. |
| `tests/Rubrics/**` | **Port, light edit** | Criteria are generic; trim to kept types. |
| `Directory.Build.props`, `.editorconfig`, `dirs.proj`, `TestProject.props` | **Rename** | Namespace prefix + globs. |
| `tools/doc-engine/*.cs`, `_schema/**` | **Rename** | Generic engine + meta-schema. |
| `governance/protected-paths` | **Edit** | Owner, tiers, prune/add rows per subsystem. |
| `governance/identity-check.sh` | **Edit** | Bot + owner identity. |
| `.github/workflows/ci.yml` | **Edit** | Check names, dotnet version, secrets. |
| `tests/Central/Arch/**`, `Structure/**` | **Re-author** | Shape ports; Rules describe *your* architecture. |
| `tools/doc-engine/{doctypes,blocks,kinds}/**` | **Re-author** | Keep `rulebook`/`rubric`; adapt ADR/plan doctypes + blocks. |
| `tests/Fixtures/**` (`Op`, `OpFlow`) | **Re-author or drop** | Product-shaped examples. |
| `governance/notify*` | **Defer (optional)** | Add once you want alerts. |
| `CLAUDE.md` "Repo controls" section | **Rewrite** | Point at the new repo's paths. |

---

*Placeholders `<NEW>`, `<owner>`, `<bot>` are defined in [Step 0](#step-0--decisions-before-you-touch-a-file). When in doubt about a Rule's or doctype's content, re-author rather than copy — that's the line that keeps this a rebuild.*
