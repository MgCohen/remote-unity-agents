# Migrating Governance to a New Repo

> A cold-readable guide for lifting **governance** — and the four subsystems it manages
> (Test Harness · Doc Engine · CODEOWNERS/CI · Hooks) — out of `abox-server` into a
> repository built from scratch.

**Before you start.** No prior `abox-server` knowledge is assumed. Two rules frame the whole port:

- **Port the machinery, re-author the content.** The engines travel cleanly; the *rules
  they enforce* (your architecture, folder names, doctypes) describe the old product —
  rewrite them, don't copy. This is what keeps it a rebuild.
- **Keep load-bearing enforcers zero-dependency.** Per [ADR 0012](#source-adrs), every guard
  in the control surface is POSIX shell with no libraries, so it behaves identically on a dev
  box, in CI, and in an agent hook. Don't "tidy" a shell guard with a dependency.

## The thesis (the frame, not a component)

Everything here is a consequence of one idea:

> **Structure over prose.** Rules are *structure the machine checks*, not prose humans (or
> agents) are trusted to follow. A guarantee encoded as a project reference, a parity check,
> or a schema fires in the build itself and is **tamper-evident** — evading it is a visible
> diff a reviewer can gate.

The thesis is the **why**. Governance and its subsystems are the **what** — the structure
that lives it:

![Governance is the main system managing four subsystems](assets/governance-model.svg)

---

## Table of contents

- [Part 1 — The model](#part-1--the-model)
  - [Governance: the main system](#governance-the-main-system)
  - [1 · Test Harness](#1--test-harness)
  - [2 · Doc Engine](#2--doc-engine)
  - [3 · CODEOWNERS / CI](#3--codeowners--ci)
  - [4 · Hooks](#4--hooks)
  - [The one seam: declarations governed, proof distributed](#the-one-seam-declarations-governed-proof-distributed)
- [Part 2 — The migration](#part-2--the-migration)
  - [Step 0 — Decisions](#step-0--decisions) · [1 — Build seam](#step-1--build--test-seam) · [2 — Governance core](#step-2--governance-core) · [3 — Test Harness](#step-3--test-harness) · [4 — Central tests](#step-4--central-tests-re-author) · [5 — Doc Engine](#step-5--doc-engine) · [6 — CI + Hooks + ruleset](#step-6--codeowners--ci--hooks--the-github-ruleset)
- [Reference](#reference) — [A · Verification](#appendix-a--verification-checklist) · [B · Rename & config](#appendix-b--rename--config-reference) · [C · File inventory](#appendix-c--file-inventory) · [Source ADRs](#source-adrs)

---

# Part 1 — The model

## Governance: the main system

**Governance is the main system.** Its core is tiny: **one policy file**
(`protected-paths`) that declares what's load-bearing, plus **one shared checker**
(`protected-paths-check.sh`) and a **CODEOWNERS generator**. On that core it **manages four
subsystems** — each reads the same policy through the same checker, so changing the policy
once keeps all four in sync. That single source of truth is what makes governance a *system*,
not a pile of scripts.

| # | Subsystem | Governs | In one line |
|---|---|---|---|
| **1** | **Test Harness** | test guarantees | Declare a guarantee as a **Rule**; prove it with a co-located `[Rule]` test; parity keeps them in lockstep. |
| **2** | **Doc Engine** | document structure | Structure-over-prose for docs: kinds → doctypes → block instances → a validator. Defines the shape of a Rulebook itself. |
| **3** | **CODEOWNERS / CI** | what merges | Required code-owner review is the **merge gate of record**; CI adds tier labels, a critical-path alert, and the identity split. |
| **4** | **Hooks** | reactions | One surface for every repo + agent event (a commit, a turn ending) → declarative `.hook` reactions, any source. |

The four sections below follow one template: **what it is · what it does · why it matters ·
key files · one diagram.**

---

## 1 · Test Harness

**What it is.** The subsystem that makes test guarantees structural. Each *kind* of test owns
a **Rulebook** — a file whose `### ` headers are plain-English **Rules**. Every Rule must have
matching test code (`[Rule("<header>")]`), and the engine fails the build if the two drift apart.

**What it does.**

- **Parity** — every `### ` header has ≥1 `[Rule]` test; every `[Rule]` cites a real header.
- **Taxonomy** — every test lives in a registered type (Arch, Structure, Unit, E2E, Wire, Live, Docs); a stray folder fails the build.
- **Co-location** — a feature's tests live *with* the feature; discovery is by location, so adding a feature needs no central wiring.

**Why it matters.** The suite is self-documenting and self-policing — you can't drop a
guarantee by accident because parity goes red. It's a **ratchet**: easy to tighten,
deliberately hard to loosen.

![Test Harness — the Rulebook ratchet](assets/harness-flow.svg)

| File / folder | Role |
|---|---|
| `tests/Harness/` | Shared base: `[Rule]`/`[LiveFact]` attributes, `Report`, `RepoTree` locator. |
| `tests/Harness/Tests/` | The engine: `ParityGuard`, `TestTypes`, `Suites`. |
| `tests/Central/` | The ownerless structural suites: `Arch`, `Structure`, `Docs`. |
| `tests/Rubrics/` | Per-type criteria a Rulebook's Rules are graded against. |
| `dirs.proj` | Test-discovery seam — globs every test project for `dotnet test dirs.proj`. |

---

## 2 · Doc Engine

**What it is.** Structure-over-prose applied to **documents**. A standalone .NET tool
(`ABox.DocEngine`, the `docengine` CLI, deliberately *not* in the solution) with a data-defined
catalog: a meta-schema → **kinds** → **doctypes** → **block instances**, plus a validator. It
defines the *shape* of a document and validates real documents in place.

**What it does.**

- **Distill + validate** — a freeform dump becomes a block `instance.md` that must conform to its doctype.
- **The on-change pipeline** — when a doc changes, **validate → checks** (both *block*) then **reviewers** (fresh agents, *advise*). It is triggered by a **Hooks** event (see [Subsystem 4](#4--hooks)) via `tools/doc-engine/on-doc-change.hook` (`mode: check`), and any doc may carry an `onChange` handler pointer.
- **Defines the Rulebook itself** — the `rulebook` and `rubric` are doctypes ([ADR 0015](#source-adrs)), so Doc Engine is the schema floor beneath the Test Harness *and* validates ADRs and plans the same way.

**Why it matters.** It's the reason "structured, not prose" is enforceable for docs, not just
a style preference — a malformed Rulebook or ADR fails a check, in place.

![Doc Engine — distill, validate, and the on-change pipeline](assets/docengine-flow.svg)

| File / folder | Role |
|---|---|
| `tools/doc-engine/_schema/` | The meta-schema — the floor the whole catalog conforms to. |
| `tools/doc-engine/kinds/`, `blocks/`, `doctypes/` | The data-defined catalog (incl. the `rulebook` + `rubric` doctypes). |
| `tools/doc-engine/*.cs` | The CLI: `SchemaChecker`, `InstanceParser`, `DocValidator`, `Catalog`, `Outline`. |
| `tools/doc-engine/on-doc-change.hook` | The Hooks manifest that fires the on-change pipeline. |

---

## 3 · CODEOWNERS / CI

**What it is.** The **server-side** half of governance — what decides what can merge, and makes
protected-path changes visible in CI.

**What it does.**

- **CODEOWNERS** — generated from `protected-paths`; required code-owner review is the **merge gate of record** (a CI check can't tell an *approved* change from an unreviewed one; a required review can).
- **`policy-guard` CI job** — verifies CODEOWNERS is in sync (this step *can* fail the build) and *advisorily* annotates + **tier**-labels PRs. Tiers are `review` (default) / `attention` / `critical`; the label is a *projection* of the policy — anything that gates must recompute from `protected-paths`, never trust the label.
- **Identity split** — a **non-admin bot** authors PRs; the owner approves. This exists to close the *solo-account paradox*: approvals key on the account, so one shared identity can't enforce anti-self-merge ([ADR 0010](#source-adrs)). `identity-check.sh` makes a throwaway probe commit and reads author *and* committer back, rejecting the owner/empty identity — and is itself wired as a gated Live test.
- **Notifier** (optional, fail-safe) — a push alert when a `critical`-tier path changes; runs inside `policy-guard` but is deliberately independent of it (a missed alert never blocks a merge).

![One policy drives every enforcer](assets/governance-flow.svg)

| File | Role |
|---|---|
| `.github/CODEOWNERS` | Generated owner map — the merge gate. Never hand-edit; regenerate. |
| `.github/workflows/ci.yml` (`policy-guard`) | CODEOWNERS-sync check + advisory annotations & tier labels. |
| `governance/identity-check.sh` | Proves commits are the bot, never the owner. |
| `governance/notify*` | Optional critical-path push alerts (Apprise/ntfy). |

---

## 4 · Hooks

**What it is.** The **unified reaction surface** — one place where *any* repository or agent
lifecycle event triggers a reaction, agentic or not. A standalone tool (`tools/hooks`, the
`abox-hooks` CLI) discovers declarative **`.hook`** files on disk and dispatches them. This is
the central point the user-facing subsystems plug into — the protected-path guard and Doc
Engine's on-change validation are both just *consumers* of it.

**What it does.**

- **Events, source-agnostic** — kinds include `CommitLanded` (git) and `TurnEnded` (a Claude turn); the model spans `SessionBegan / PromptSubmitted / ToolPending / ToolDone / AwaitingInput` too, across sources `{git, claude, codex}`. Only `CommitLanded`/`TurnEnded` are wired producers today.
- **Declarative, zero-build** — drop `<name>.hook` in any feature folder; it's discovered by globbing, no registration. It names `on:` event kinds, an optional `when:` filter (`source` / `cwd glob` / `tool`), a `mode:`, and one action: `run:` (a shell command, event delivered as JSON on stdin — any language) **or** `agent:` (a fresh reviewer agent).
- **Modes** — `notify` (async, result ignored) · `check` (synchronous: output is fed back to the running agent, and a non-zero exit **blocks the turn from ending**). *(`gate` is parseable but not yet dispatched.)*
- **Opt-in + transport** — a repo opts in with an `.abox/` directory; events append to `.abox/hooks.jsonl` and dispatch incrementally via a cursor. `abox-hooks install-git` writes `.git/hooks/post-commit`; `install-claude` writes the Claude Stop hook.

**Why it matters.** Every reaction — validate a doc, guard a protected path, spawn a reviewer —
lives behind **one seam** in any language, so you add a repo behavior by dropping a file, not
wiring a framework. It's the opposite of scattered per-tool hooks.

![Hooks — one surface for every repo & agent reaction](assets/hooks-flow.svg)

> **Two "hooks", don't conflate them.** `tools/hooks` is the reaction *engine* above.
> Separately, governance ships two static shell guards in **`.githooks/`** (`pre-commit` /
> `pre-push`) — the always-on protected-path catch, enabled with
> `git config core.hooksPath .githooks`. Related idea, different mechanism: `abox-hooks
> install-git` writes to `.git/hooks/`, not `.githooks/`.

| File | Role |
|---|---|
| `tools/hooks/` (`ABox.Hooks`, `abox-hooks`) | The reaction engine: catalog, manifest parser, dispatcher, git/Claude installers. |
| `<feature>/*.hook` | Declarative reaction manifests, co-located with the feature. |
| `.githooks/pre-commit`, `pre-push` | Static protected-path local catch (separate from the engine). |
| `.gitattributes` | Pins the shell guards to LF so a CRLF checkout can't break them. |

**Porting gotchas** (preserve these): the `check` protocol is Claude's Stop envelope — exit 2 +
stderr *blocks* and feeds the reason back; exit 0 + `additionalContext` *advises* (capped
~10k chars). Two loop guards must survive the port: `stop_hook_active` downgrades a block to
context, and `ABOX_HOOKS_SUPPRESS=1` stops an `agent:`-spawned reviewer's own turn-end from
re-triggering its spawner.

---

## The one seam: declarations governed, proof distributed

The single most important relationship to carry across:

> **Declarations are governed; proof is distributed.** Every Rulebook, rubric, and doctype is a
> **protected path** — central, owner-reviewed. The Rules *live inside governance*. The only
> thing that lives **outside** the wall is the specific `[Rule]` test code, co-located with the
> feature it protects.

That split is why the Test Harness and Doc Engine interlock — a Rulebook is one doctype spread
across three layers:

| Layer | What it is | Home |
|---|---|---|
| **Doctype** (`rulebook`, `rubric`) | the schema — what *any* Rulebook/rubric must look like | central — `tools/doc-engine/doctypes/` |
| **`<type>.md`** | the per-type criteria ("what a Unit test is") | central — `tests/Rubrics/` |
| **`Rulebook.md`** | this feature's guarantees | co-located with the feature |

```
governance/protected-paths ──guards──► Rulebooks · rubrics · doctypes · CI · build config
   (the declarations)                        │
tests/**/*.cs [Rule] facts ◄── prove ────────┘   (the proof — co-located, NOT protected)
```

So renaming or dropping a subsystem means the **policy rows** follow: the declarations and
their guard move together.

---

# Part 2 — The migration

One unified path, walking-skeleton first. Do the steps in order — each leaves the repo in a
buildable state.

![Migration order — seven steps, Doc Engine deferrable](assets/steps-rail.svg)

> **Each step carries four cues:** **Goal** · what to **Copy** · what to **Edit** · how to **Verify**.

## Step 0 — Decisions

Settle these first; they change *what* you port. Every subsystem is core to the model — the
real question is **sequencing** (v1 vs later).

| Decision | Options | Recommended |
|---|---|---|
| **Doc Engine** | Port `tools/doc-engine` now **or** defer (the ratchet still works via parity; you lose in-place doc validation + the `Docs` type until it lands). | **Defer past the first green build**, then port — largest single piece. *Not dropped* — a core subsystem. |
| **`Docs` test type** | Register now (needs Doc Engine) **or** when Doc Engine lands. | Land it **with** Doc Engine (Step 5). |
| **Critical-path notifier** | Wire `notify*` (needs an ntfy/Apprise channel + secret) **or** defer. | **Defer.** Keep the gate + hooks; add alerts later. |
| **`.claude` PreToolUse guard** | Wire a Claude hook that calls the checker **or** rely on hooks + CI ([ADR 0007](#source-adrs)). | **Wire it** if you use Claude Code — cheap local backstop. |
| **Live tests** | Keep the gated real-CLI `Live` type **or** drop until you have a CLI. | Keep the *type*, no cases — skipped without `RUN_LIVE=1`. |

**Pick your names now** (used everywhere downstream):

| Placeholder | Meaning | Example |
|---|---|---|
| `<NEW>` | Product namespace prefix (replaces `ABox`) | `Acme` |
| `<owner>` | GitHub handle that reviews protected paths | `@your-handle` |
| `<bot>` | Machine account that authors agent commits | `Acme-Agent` |

> **Deferring Doc Engine** means: leave `"Docs"` out of `TestTypes.Registered`, and drop the four
> `tools/doc-engine/{doctypes,blocks,kinds,_schema}/**` rows from `protected-paths` for now (the
> `tests/Rubrics/**` glob stays). Add them back in Step 5.

## Step 1 — Build & test seam

- **Goal** — a new repo that builds and runs an (empty) test pass.
- **Copy** (renaming): `<NEW>.slnx` (was `ABox.slnx`, the solution *and* repo-root marker);
  `Directory.Build.props`, `.editorconfig`, `.gitattributes`, `.gitignore`; `dirs.proj`,
  `tests/TestProject.props`.
- **Edit** — the namespace derivation prefix `ABox.` → `<NEW>.` in `Directory.Build.props`;
  confirm the glob paths (`src/**`, `tests/**`, `tools/**`) in `dirs.proj`.
- **Verify** — `dotnet build <NEW>.slnx` succeeds.

## Step 2 — Governance core

- **Goal** — the spine every subsystem reads: one policy, one checker, one generator.
- **Copy** — `governance/protected-paths`, `protected-paths-check.sh`, `generate-codeowners.sh`.
- **Edit** — owner `@MgCohen` → `<owner>` in `protected-paths`; prune rows for anything not built
  yet (add them back as you land each subsystem); optionally rename `ABOX_ALLOW_PROTECTED` →
  `<NEW>_ALLOW_PROTECTED` (in the checker + README; the hooks just call the checker).
- **Verify** — `./governance/generate-codeowners.sh` writes `.github/CODEOWNERS` with no diff on
  re-run; `printf 'CLAUDE.md\n' | ./governance/protected-paths-check.sh` exits non-zero.

## Step 3 — Test Harness

- **Goal** — the parity engine builds and finds the repo root.
- **Copy** — `tests/Harness/**` and `tests/Rubrics/**`.
- **Edit** — the hardcoded couplings:

  | File | Change |
  |---|---|
  | `tests/Harness/RepoTree.cs` | `Marker = "ABox.slnx"` → `"<NEW>.slnx"` |
  | `tests/Harness/TestAssemblies.cs` | `Prefix`, `SharedPrefix` (`"ABox."`, `"ABox.Tests."`) |
  | `tests/Harness/Tests/TestTypes.cs` | Namespace + the `Registered` set (leave out `Docs` until Step 5) |
  | All `.cs` under `tests/Harness/` | `namespace ABox.Tests.*` → `<NEW>.Tests.*` (IDE0130 forces this) |
  | The three csprojs + `TestProject.props` | `AssemblyName` / `RootNamespace` / `<Using Include="ABox.Tests.Harness"/>` |

- **Verify** — `dotnet build tests/Harness/Tests/<NEW>.Tests.Harness.Tests.csproj` succeeds and
  `RepoTree` finds the marker (no "could not locate repo root" throw).

## Step 4 — Central tests (re-author)

- **Goal** — `Arch` and `Structure` suites describing **your** architecture, green under parity.
- **Copy** — `tests/Central/` **shape**: the per-type layout (`<Type>/Rulebook.md`, the `.cs`,
  `<Type>/Support/`) and `SuiteAnchor`.
- **Re-author** ⚠️ (not copy) — the Rules encode `abox-server`'s layer graph and folders:
  - `Arch/Support` — your layer allow-graph.
  - `Structure/Support` — your `src/` home folders.
  - `Rulebook.md` files — rewrite each `### ` Rule for *your* invariants + its `[Rule("…")]` fact.
- **Edit** — csproj `AssemblyName`/`RootNamespace` → `<NEW>.Tests.Central`; the production glob
  `src\**\ABox.*.csproj` → `<NEW>.*.csproj`.
- **Verify** — `dotnet test dirs.proj` is green; parity passes.

## Step 5 — Doc Engine

- **Goal** — structured documents validated in place; the `Docs` type live; the on-change
  pipeline wired through Hooks.
- **Copy** — `tools/doc-engine/**` (CLI `.cs`, `_schema/`, `kinds/`, `blocks/`, `doctypes/`,
  `scripts/`, `on-doc-change.hook`).
- **Edit** — assembly/namespace `ABox.DocEngine` → `<NEW>.DocEngine`; **re-author** the catalog
  (keep `rulebook`/`rubric` doctypes; adapt your ADR/plan doctypes + blocks); re-add the four
  `tools/doc-engine/{doctypes,blocks,kinds,_schema}/**` rows + `tools/**/Tests/**/Rulebook.md` to
  `protected-paths`; register `"Docs"` in `TestTypes.Registered` and restore `tests/Central/Docs/`.
- **Verify** — `docengine check` (catalog conforms to the meta-schema) + `docengine validate <a
  Rulebook>` pass; `dotnet test dirs.proj` runs the `Docs` type green.

## Step 6 — CODEOWNERS / CI + Hooks + the GitHub ruleset

- **Goal** — the enforcers wired, and the server-side guarantees the files assume.
- **Copy** — `tools/hooks/**` (the `abox-hooks` engine) and any `.hook` files you keep;
  `.githooks/**`; the `ci.yml` `policy-guard` job; (if kept) `governance/notify*`.
- **Edit**:

  | File | Change |
  |---|---|
  | `tools/hooks/**` | Assembly/namespace `ABox.Hooks` → `<NEW>.Hooks`; keep the `.abox/` opt-in + env-var names (`ABOX_HOOKS_SUPPRESS`) or rename consistently. |
  | `governance/identity-check.sh` | `BOT_NAME`/`BOT_EMAIL` → `<bot>`; `OWNER_NAME`/`OWNER_EMAIL_MARK` → your owner. |
  | `.github/workflows/ci.yml` | Required-check names (`build-test (ubuntu/windows-latest)`), `dotnet-version`, (if kept) the `NTFY_TOPIC` secret. |

- **Configure on GitHub** (settings, not code):
  - [ ] **Branch ruleset on `main`** — require a PR; require the CI checks (`build-test (ubuntu-latest)` + `windows-latest`); require **code-owner review**; block force-push; empty bypass.
  - [ ] **Identity split** — create the non-admin `<bot>` account; require **1 approval + last-push approval** so the bot can't self-merge a protected change.
  - [ ] **Secrets** — add `NTFY_TOPIC` *only if* you kept the notifier (low-value string; never a real credential in the PR-triggered step).
- **Verify** — touching a protected path is blocked by the hook and flagged by CI; `abox-hooks
  install-git` + a commit dispatches a `CommitLanded` reaction; a bot PR on a protected path is
  blocked until `<owner>` approves.

---

# Reference

## Appendix A — Verification checklist

- [ ] `dotnet build <NEW>.slnx` — warning-free.
- [ ] `dotnet test dirs.proj` — green; parity + taxonomy guards pass.
- [ ] `RepoTree` finds the new `<NEW>.slnx` marker.
- [ ] `generate-codeowners.sh` re-run produces **no** diff.
- [ ] Editing a protected path is blocked by the git hook and flagged by CI.
- [ ] `docengine check` + `docengine validate <doc>` pass (if Doc Engine ported).
- [ ] `abox-hooks` dispatches a `.hook` on a commit / turn-end (if Hooks ported).
- [ ] `identity-check.sh` passes as `<bot>`, fails if it detects the owner identity.
- [ ] Branch ruleset blocks an unreviewed protected-path merge to `main`.
- [ ] No stray `ABox` / `@MgCohen` / `ABox-Agent` remains (`grep -ri 'abox\|mgcohen' .` clean bar intentional history).

## Appendix B — Rename & config reference

**Mechanical renames** (find/replace):

| From | To | Where |
|---|---|---|
| `ABox.slnx` | `<NEW>.slnx` | solution file + `RepoTree.cs` marker |
| `ABox.` (namespace prefix) | `<NEW>.` | all `.cs`, `Directory.Build.props`, csprojs |
| `ABox.Tests.Central/.Harness` | `<NEW>.Tests.Central/.Harness` | csprojs, `SuiteAnchor`, usings |
| `ABox.DocEngine` | `<NEW>.DocEngine` | doc-engine csproj + namespaces |
| `ABox.Hooks` | `<NEW>.Hooks` | repo-hooks csproj + namespaces |
| `ABox.*.csproj` (glob) | `<NEW>.*.csproj` | `tests/Central` production ProjectReference |

**Real config edits** (judgment, not find/replace):

| Setting | File | Change |
|---|---|---|
| Owner handle | `governance/protected-paths` | `@MgCohen` → `<owner>` (then regenerate) |
| Bot identity | `governance/identity-check.sh` | `BOT_*`, `OWNER_*` |
| Registered types | `tests/Harness/Tests/TestTypes.cs` | add `Docs` when Doc Engine lands |
| Required checks | `.github/workflows/ci.yml` + ruleset | must match the job names exactly |
| Protected paths | `governance/protected-paths` | add each subsystem's rows as you land it |

## Appendix C — File inventory

| Path | Action | Note |
|---|---|---|
| `governance/protected-paths-check.sh`, `generate-codeowners.sh` | **Port as-is** | Generic; zero coupling. |
| `.githooks/**`, `.gitattributes` | **Port as-is** | Path literals only. |
| `tools/hooks/**` (`ABox.Hooks`) | **Rename** | Namespace + env-var names. |
| `tests/Harness/**` | **Rename** | Namespace + marker + prefixes. |
| `tests/Rubrics/**` | **Port, light edit** | Trim to kept types. |
| `Directory.Build.props`, `.editorconfig`, `dirs.proj`, `TestProject.props` | **Rename** | Namespace prefix + globs. |
| `tools/doc-engine/*.cs`, `_schema/**` | **Rename** | Generic engine + meta-schema. |
| `governance/protected-paths` | **Edit** | Owner, tiers, prune/add rows. |
| `governance/identity-check.sh` | **Edit** | Bot + owner identity. |
| `.github/workflows/ci.yml` | **Edit** | Check names, dotnet version, secrets. |
| `tests/Central/Arch/**`, `Structure/**` | **Re-author** | Rules describe *your* architecture. |
| `tools/doc-engine/{doctypes,blocks,kinds}/**` | **Re-author** | Keep `rulebook`/`rubric`; adapt ADR/plan doctypes. |
| `tests/Fixtures/**` (`Op`, `OpFlow`) | **Re-author or drop** | Product-shaped examples. |
| `governance/notify*` | **Defer (optional)** | Add once you want alerts. |
| `CLAUDE.md` "Repo controls" section | **Rewrite** | Point at the new repo's paths. |

## Source ADRs

Read these for the *why* behind the machinery (they outlive any single layer):

| ADR | Why it matters to the port |
|---|---|
| **0010** — agent repo controls | The foundational record: one-policy-many-enforcers, the enforcer ranking, the machine-account split. |
| **0012** — dependency budget by failure mode | The porting rule: load-bearing enforcers stay zero-dependency POSIX shell; only fail-safe convenience (the notifier) may take a library. |
| **0007** / 0006 — PreToolUse / Claude Stop | The provider-hook substrate the Hooks `TurnEnded` producer and the Step-0 PreToolUse decision build on. |
| **0015** (+ 0016/0017) — Rulebook doctype | The three-layer Rulebook and the doc-engine catalog shape you re-author. |

---

*Placeholders `<NEW>`, `<owner>`, `<bot>` are defined in [Step 0](#step-0--decisions). When in
doubt about a Rule's or doctype's content, re-author rather than copy — that's the line that
keeps this a rebuild.*
