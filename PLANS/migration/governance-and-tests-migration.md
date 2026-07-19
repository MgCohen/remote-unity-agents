# Migrating Governance & the Test Harness to a New Repo

**What this is.** A self-contained guide for lifting two pieces of infrastructure —
the **governance / repo-controls** system and the **Rulebook test harness** — out of
`abox-server` and into a fresh repository being built from scratch.

**Who it's for.** Someone standing up the new repo who has *not* seen `abox-server`
before. No prior context assumed. Read Part 1 to understand *what* you're bringing and
*why*; follow Part 2 to actually do it.

**Ground rule.** We are porting **infrastructure**, not product behavior. The machinery
(shell guards, build config, the parity engine) travels cleanly. The *content* those
tools enforce — the specific architecture rules, folder names, fixtures — describes the
old product and must be **re-authored**, not copied. This keeps the port on the right
side of the "rebuild, don't reuse" line.

Throughout, `<NEW>` is a placeholder for your new product's namespace prefix (what
`ABox` becomes). A full substitution table is in [§4](#4--rename--config-reference).

---

## Table of contents

- [Part 1 — Context: what we're bringing and why](#part-1--context-what-were-bringing-and-why)
  - [1.1 Governance — "repo controls"](#11-governance--repo-controls)
  - [1.2 The test harness — "Rulebook discipline"](#12-the-test-harness--rulebook-discipline)
  - [1.3 How the two connect](#13-how-the-two-connect)
- [Part 2 — The migration](#part-2--the-migration)
  - [Step 0 — Decisions before you touch a file](#step-0--decisions-before-you-touch-a-file)
  - [Step 1 — Lay the build & test seam](#step-1--lay-the-build--test-seam)
  - [Step 2 — Port the harness engine](#step-2--port-the-harness-engine)
  - [Step 3 — Port the central tests (re-author the content)](#step-3--port-the-central-tests-re-author-the-content)
  - [Step 4 — Port governance](#step-4--port-governance)
  - [Step 5 — Configure the GitHub side](#step-5--configure-the-github-side)
  - [Step 6 — Optional extras](#step-6--optional-extras)
- [§3 — Verification: done-when checklist](#3--verification-done-when-checklist)
- [§4 — Rename & config reference](#4--rename--config-reference)
- [§5 — File inventory: port / rename / re-author / drop](#5-file-inventory-port--rename--re-author--drop)

---

# Part 1 — Context: what we're bringing and why

Two independent subsystems. Neither touches product logic, so both can move before your
new codebase has any real features.

| Subsystem | In one line | Why bring it |
|---|---|---|
| **Governance** | One policy file guards your "load-bearing" paths through several enforcers. | Stops any agent (or a careless commit) from quietly weakening your test/CI/build guardrails. |
| **Test harness** | Every test states its guarantee in English and is checked to have matching test code. | Tests can't silently rot — a guarantee without a test (or a test citing nothing) fails the build. |

## 1.1 Governance — "repo controls"

**What it is.** A single declarative list of paths that are dangerous to change —
the test harness, CI config, build settings, the policy itself. One file is the source
of truth; several **enforcers** read it and react.

**What it does.**

- Names each protected path with an **owner**, a **severity tier**, and a **reason**.
- Turns that list into a GitHub **CODEOWNERS** merge gate (the real guarantee).
- Runs a **CI job** (`policy-guard`) that verifies CODEOWNERS is in sync (this step can
  fail the build) and *advisorily* annotates + labels PRs touching protected paths.
- Provides **git hooks** that catch protected edits locally, before they're even pushed.
- Optionally fires a **push notification** when a `critical`-tier path changes.

**Why we want it.** In an agent-driven repo, the enforcement surface is exactly what an
agent might "fix" to make a red build green. This makes editing that surface a
deliberate, reviewed act — the block *is* the feature working.

![Governance flow — one policy, many enforcers](assets/governance-flow.svg)

**Key pieces (high level):**

| File | Role |
|---|---|
| `governance/protected-paths` | The policy. `glob \| owner \| tier \| reason`, one rule per line. |
| `governance/protected-paths-check.sh` | The one checker every enforcer calls. |
| `governance/generate-codeowners.sh` | Regenerates `.github/CODEOWNERS` from the policy. |
| `.githooks/pre-commit`, `pre-push` | Local catch (opt-in per clone). |
| `.github/workflows/ci.yml` (`policy-guard` job) | CODEOWNERS-sync check (blocks) + advisory annotations & PR tier labels. |
| `governance/identity-check.sh` | Proves commits are authored by the bot account, never the owner. |
| `governance/notify*` | Optional critical-path push alerts (via Apprise/ntfy). |

## 1.2 The test harness — "Rulebook discipline"

**What it is.** A convention plus a small engine. Each *kind* of test owns a
**Rulebook** — a markdown file whose `### ` headers are plain-English **guarantees**.
Each guarantee must have matching test code (`[Rule("<header>")]`), and the engine
fails the build if the two ever drift apart.

**What it does.**

- **Parity:** every `### ` header has ≥1 `[Rule]` test; every `[Rule]` cites a real
  header. No orphans on either side.
- **Taxonomy:** every test lives in a **registered type** (Arch, Structure, Unit, E2E,
  Wire, Live, Docs). A stray folder fails the build.
- **Co-location:** a feature's tests live *with* the feature; the engine discovers them
  by location, so adding a feature needs no central wiring.

**Why we want it.** It makes the test suite self-documenting and self-policing. You read
the Rulebook to know what's guaranteed; you can't delete a guarantee by accident because
parity goes red. It's a **ratchet** — easy to tighten, deliberately hard to loosen.

![Harness flow — the Rulebook ratchet](assets/harness-flow.svg)

**Key pieces (high level):**

| File / folder | Role |
|---|---|
| `tests/Harness/` | Shared base: `[Rule]`/`[LiveFact]` attributes, `Report`, `RepoTree` locator. |
| `tests/Harness/Tests/` | The engine + its own tests: `ParityGuard`, `TestTypes`, `Suites`. |
| `tests/Central/` | The ownerless structural suites: `Arch`, `Structure`, `Docs`. |
| `tests/Rubrics/` | Per-type criteria a Rulebook's Rules are graded against. |
| `dirs.proj` | The test-discovery seam — globs every test project so `dotnet test dirs.proj` runs them all. |
| `Directory.Build.props`, `.editorconfig` | Namespace-mirrors-folder law + build conventions. |

## 1.3 How the two connect

They're mostly independent, but two seams link them — know these before you cut anything:

1. **Governance protects the harness.** `protected-paths` lists `tests/Harness/**`,
   `tests/**/Rulebook.md`, `tests/Rubrics/**`, etc. When you rename or drop harness
   pieces, the policy rows must follow.
2. **The Docs test type depends on a tool.** The `Docs` type shells out to a separate
   `tools/doc-engine` to validate structured documents. If you don't port that tool,
   drop the `Docs` type (see [Step 0](#step-0--decisions-before-you-touch-a-file)).

```
governance/protected-paths ──guards──> tests/Harness, Rulebooks, Rubrics, CI, build config
tests/Central/Docs ──shells out to──> tools/doc-engine   (port together, or drop together)
```

---

# Part 2 — The migration

One unified path. Do the steps in order — each leaves the repo in a buildable state.

> **Legend for every step:** 🎯 goal · 📦 what moves · ✏️ edits · ✅ verify.

## Step 0 — Decisions before you touch a file

Settle these first; they change *what* you port.

| Decision | Options | Recommended for v1 |
|---|---|---|
| **Docs type + `doc-engine`** | Port the whole `tools/doc-engine` **or** drop the `Docs` type. | **Drop.** Add it back once you have structured docs to validate. |
| **Critical-path notifier** | Port `notify*` (needs an ntfy/Apprise channel + secret) **or** drop it. | **Drop.** Keep the CODEOWNERS gate + hooks; add alerts later. |
| **`.claude` PreToolUse guard** | Wire a Claude hook that calls the checker **or** rely on git hooks + CI. | **Wire it** if you use Claude Code — cheap local backstop. |
| **Live tests** | Keep the gated real-CLI `Live` type **or** drop until you have a CLI to drive. | Keep the *type*, no cases yet — it stays skipped without `RUN_LIVE=1`. |

**Pick your names now** (used everywhere downstream):

| Placeholder | Meaning | Example |
|---|---|---|
| `<NEW>` | Product namespace prefix (replaces `ABox`) | `Acme` |
| `<owner>` | GitHub handle that reviews protected paths | `@your-handle` |
| `<bot>` | Machine account that authors agent commits | `Acme-Agent` |

> **Dropping `Docs`** means: delete `tests/Central/Docs/` and `tests/Rubrics/Docs.md`,
> remove `"Docs"` from `TestTypes.Registered`, and delete the four
> `tools/doc-engine/{doctypes,blocks,kinds,_schema}/**` rows from `protected-paths` (the
> `tests/Rubrics/**` glob stays — it still covers your remaining types). The
> parity/taxonomy guards then expect exactly the types you kept.

## Step 1 — Lay the build & test seam

🎯 A new repo that builds and runs an (empty) test pass.

📦 Copy these, renaming as you go:

- `<NEW>.slnx` (was `ABox.slnx`) — the solution *and* the repo-root marker.
- `Directory.Build.props`, `.editorconfig`, `.gitattributes`, `.gitignore`
- `dirs.proj`, `tests/TestProject.props`

✏️ Edits:

- In `Directory.Build.props`, change the namespace derivation prefix `ABox.` → `<NEW>.`
- In `dirs.proj` and the props, confirm the glob paths (`src/**`, `tests/**`,
  `tools/**`) match your intended layout.

✅ `dotnet build <NEW>.slnx` succeeds (nothing to compile yet, but config is valid).

## Step 2 — Port the harness engine

🎯 The parity engine builds and can find the repo root.

📦 Copy `tests/Harness/**` and `tests/Rubrics/**`.

✏️ Edits — these are the **hardcoded couplings** (there are only a few):

| File | Change |
|---|---|
| `tests/Harness/RepoTree.cs` | `Marker = "ABox.slnx"` → `"<NEW>.slnx"` |
| `tests/Harness/TestAssemblies.cs` | `Prefix`, `SharedPrefix` (`"ABox."`, `"ABox.Tests."`) |
| `tests/Harness/Tests/TestTypes.cs` | Namespace + the `Registered` set (drop `Docs` if chosen) |
| All `.cs` under `tests/Harness/` | `namespace ABox.Tests.*` → `<NEW>.Tests.*` (IDE0130 forces this) |
| The three csprojs + `TestProject.props` | `AssemblyName` / `RootNamespace` / `<Using Include="ABox.Tests.Harness"/>` |

✅ `dotnet build tests/Harness/Tests/<NEW>.Tests.Harness.Tests.csproj` succeeds and the
`RepoTree` locator finds your new marker (no "could not locate repo root" throw).

## Step 3 — Port the central tests (re-author the content)

🎯 `Arch` and `Structure` suites that describe **your** architecture, green under parity.

📦 Copy `tests/Central/` **shape** — the per-type folder layout (`<Type>/Rulebook.md`,
the `.cs`, `<Type>/Support/`) and `SuiteAnchor`.

> ⚠️ **This is the re-author step, not a copy step.** The *Rules themselves* encode
> `abox-server`'s layer graph and folder names. Keep the structure; rewrite the content:

- **`Arch/Support`** — replace the layer allow-graph with your architecture's.
- **`Structure/Support`** — replace the "home folders" set with your `src/` layout.
- **`Rulebook.md` files** — rewrite each `### ` Rule to state *your* invariants, then
  write the matching `[Rule("…")]` fact. Start with a handful; add liberally later.

✏️ Edits: csproj `AssemblyName`/`RootNamespace` → `<NEW>.Tests.Central`; the production
`ProjectReference` glob `src\**\ABox.*.csproj` → `<NEW>.*.csproj`.

✅ `dotnet test dirs.proj` runs and is green; parity passes (every Rule has a fact, every
fact cites a Rule).

## Step 4 — Port governance

🎯 The policy + enforcers, pointed at your paths and handles.

📦 Copy `governance/**`, `.githooks/**`, and the `.github/workflows/ci.yml`
`policy-guard` job.

✏️ Edits:

| File | Change |
|---|---|
| `governance/protected-paths` | Owner `@MgCohen` → `<owner>`; prune rows for anything you didn't port (`src/Api/**`, and the four `tools/doc-engine/*` rows if `Docs` dropped); confirm each glob matches your real layout. |
| `governance/identity-check.sh` | `BOT_NAME`/`BOT_EMAIL` → `<bot>`; `OWNER_NAME`/`OWNER_EMAIL_MARK` → your owner. |
| `.github/workflows/ci.yml` | Required-check names (`build-test (ubuntu/windows-latest)`), `dotnet-version`, and (if kept) the `NTFY_TOPIC` secret. |
| `ABOX_ALLOW_PROTECTED` env var | Optional rename to `<NEW>_ALLOW_PROTECTED` — it lives in the checker and README (the hooks just call the checker). |

Then **regenerate** (never hand-edit CODEOWNERS):

```sh
./governance/generate-codeowners.sh
```

✅ Touch a protected path on a branch → the pre-commit hook blocks it; setting the
override env var lets it through and logs the override. `generate-codeowners.sh` produces
a `.github/CODEOWNERS` with no diff on re-run.

## Step 5 — Configure the GitHub side

🎯 The server-side guarantees the files *assume* exist. These are repo settings, not code.

- [ ] **Branch ruleset on `main`** — require a PR before merge; require the CI checks
      (`build-test (ubuntu-latest)` + `windows-latest`); require **code-owner review**;
      block force-push; empty bypass list.
- [ ] **Identity split** — create the `<bot>` machine account (non-admin), have it author
      PRs, and require **1 approval + last-push approval** so the bot can't self-merge a
      protected change.
- [ ] **Secrets** — add `NTFY_TOPIC` *only if* you kept the notifier (it's a low-value
      string, safe in PR CI; never put a real credential in the PR-triggered step).

✅ A PR from the bot touching a protected path is blocked until `<owner>` approves.

## Step 6 — Optional extras

Only if you chose them in Step 0.

- **`doc-engine` + `Docs` type** — port `tools/doc-engine/**`, restore `"Docs"` to
  `TestTypes.Registered` and `tests/Central/Docs/`, and re-add the doc-engine rows to
  `protected-paths`. Inventory the tool separately; it's the largest single piece.
- **Notifier** — port `notify.yml` / `notify-critical.sh`, add an Apprise channel URL,
  wire the secret into the alert step's `env:` block, subscribe your device.

---

# §3 — Verification: done-when checklist

- [ ] `dotnet build <NEW>.slnx` — warning-free.
- [ ] `dotnet test dirs.proj` — green; parity + taxonomy guards pass.
- [ ] `RepoTree` finds the new `<NEW>.slnx` marker.
- [ ] Editing a protected path is blocked by the git hook and flagged by CI.
- [ ] `generate-codeowners.sh` re-run produces **no** diff.
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
| `ABox.*.csproj` (glob) | `<NEW>.*.csproj` | `tests/Central` production ProjectReference |

**Real config edits** (judgment, not find/replace):

| Setting | File | Change |
|---|---|---|
| Owner handle | `governance/protected-paths` | `@MgCohen` → `<owner>` (then regenerate) |
| Bot identity | `governance/identity-check.sh` | `BOT_*`, `OWNER_*` |
| Registered types | `tests/Harness/Tests/TestTypes.cs` | drop `Docs` if not porting the tool |
| Required checks | `.github/workflows/ci.yml` + ruleset | must match the job names exactly |
| Protected paths | `governance/protected-paths` | prune rows for unported paths |

# §5 — File inventory: port / rename / re-author / drop

| Path | Action | Note |
|---|---|---|
| `governance/protected-paths-check.sh`, `generate-codeowners.sh` | **Port as-is** | Generic; zero product coupling. |
| `.githooks/**`, `.gitattributes` | **Port as-is** | Path literals only. |
| `tests/Harness/**` | **Rename** | Namespace + marker + prefixes. |
| `tests/Rubrics/**` | **Port, light edit** | Criteria are generic; trim to kept types. |
| `Directory.Build.props`, `.editorconfig`, `dirs.proj`, `TestProject.props` | **Rename** | Namespace prefix + globs. |
| `governance/protected-paths` | **Edit** | Owner, tiers, prune rows. |
| `governance/identity-check.sh` | **Edit** | Bot + owner identity. |
| `.github/workflows/ci.yml` | **Edit** | Check names, dotnet version, secrets. |
| `tests/Central/Arch/**`, `Structure/**` | **Re-author** | Shape ports; Rules describe *your* architecture. |
| `tests/Fixtures/**` (`Op`, `OpFlow`) | **Re-author or drop** | Product-shaped examples. |
| `tests/Central/Docs/**`, `tools/doc-engine/**` | **Drop (v1)** | Port later with the tool. |
| `governance/notify*` | **Drop (v1)** | Optional convenience. |
| `CLAUDE.md` "Repo controls" section | **Rewrite** | Point at the new repo's paths. |

---

*Placeholders `<NEW>`, `<owner>`, `<bot>` are defined in [Step 0](#step-0--decisions-before-you-touch-a-file). When in doubt about a Rule's content, re-author rather than copy — that's the line that keeps this a rebuild.*
