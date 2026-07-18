# ICS — Intent · Constraints · Success

> **Status:** Methodology doc for the ICS artifact. Consolidates the two research
> passes ([`research/intent-vs-spec-driven-development.md`](research/intent-vs-spec-driven-development.md),
> [`research/authoring-locked-artifacts-2026-followup.md`](research/authoring-locked-artifacts-2026-followup.md))
> and the decisions that came out of them into one place. Sits with
> [`the-box.md`](the-box.md) as **exploration on top of** the L1→L12 rebuild — a
> proposal until promoted to a PRD/ADR. The fill-in artifact is
> [`ics-template.md`](ics-template.md); this doc is the *why and how* behind it.
> A rendered, diagram-forward version is [`ics.html`](ics.html) (open in a browser) —
> same content with the freeze, drift, gating, and nesting flows drawn out.

---

## 1. What it is

**ICS is a one-page artifact authored *before* planning that anchors a stream of
work.** Three parts:

| | Part | The question it answers | Framing |
|---|---|---|---|
| **I** | **Intent** | *Why does this work exist, and what does "done" mean at the top?* | The durable why/what + a North Star |
| **C** | **Constraints** | *Where must the agent NOT go?* | Guardrails — "don't cross these lines" |
| **S** | **Success** | *How will we know it worked?* | Observable, checkable outcomes |

It is **not a PRD, spec, or task list.** It is the *anchor* those things are derived
from and judged against. In A.Box terms, a Box's ICS is authored at **creation**
([`the-box.md`](the-box.md) §4.0), committed into `.box/`, and then does two jobs for
the rest of the Box's life:

```
  ICS  ──anchors──►  planning conversation (IPlanner)  ──►  phased plan
   │                                                          │
   └──anchors──►  ground-up review  ◄── each phase judged against C + S
```

**The mental model — a ladder, not a camp.** Research finding: *intent → spec → tests
→ code* is one ladder; the only real question is which rungs a human hand-authors and
freezes vs. which the agent regenerates. **ICS is the durable top rung.** Specs/plans
are the regenerable middle. We stay **spec-anchored, never spec-as-source** — humans
own the ICS and read the diffs; the plan below it is cheap to regenerate against it.

---

## 2. Core principles

Each principle traces to a research finding or an explicit decision. Cited so we know
*why* a rule exists, not just that it does.

### 2.1 Constraints are guardrails, not tramlines — lead with the negatives
The strongest new empirical result ("Guardrails Beat Guidance," 5,000+ Claude Code
runs on SWE-bench Verified): **every individually beneficial rule was a negative
constraint ("do not X"); every harmful one was a positive directive ("do X").** So the
**C** section is written as **prohibitions and boundaries** — what must not change,
forbidden deps, perf/security budgets, invariants to honor — not step-by-step
procedure. Procedure belongs in the plan; boundaries belong in the anchor.

### 2.2 One page, on purpose — the optimum is in the middle
Specification is now widely argued to be the real bottleneck (Eisele, Osmani): vague
specs don't remove cost, they **defer and fragment** it downstream. But Eisele's cost
curve is **U-shaped** — the optimum is *well-structured acceptance criteria in the
middle*, not maximalist lock-down. Over-authoring re-introduces the brittleness that
kills rigid spec-driven work. **Keep the ICS to a page.** If it's growing into a PRD,
you're on the wrong rung.

### 2.3 Soft-lock — the anchor is safe *because* changing it is deliberate
Freezing may not beat *mere presence* on per-task output quality (the study found
random rules matched curated rules). **But that is the wrong axis.** Freezing buys
**anchor integrity over the project's life** and **lower mental load** — you steer by
the anchor, so you need to know it's clear and stable. The failure mode it prevents:
if the artifact mutates freely, accumulated drift **leaks back into the anchor
itself**, and you steer the wrong way while still trusting it. A *present-but-corrupted*
anchor is worse than a stable one.

So the ICS is **soft-locked**:

- **No auto-mutation.** The agent never edits the ICS as a side effect of shipping.
- **Every change → human review + hard approval.** Moving a guardrail is a conscious,
  logged decision, never the first-resort move.
- **Default is work-within-the-anchor**, not amend-it.

This is not a new mechanism — it's the **protected-paths + required-review** soft-lock
the repo already applies to ADRs, the harness, and CI. The ICS inherits that
governance.

### 2.4 Success criteria are the review bar — make them checkable
Every credible case for NL fixtures keeps a **human in the review loop** and keeps the
fixture **executable/checkable**. The **S** section is the bar each phase's ground-up
review is judged against; phrase it as observable outcomes (acceptance checks, parity
gates, Given/When/Then on the critical path) — not "it works well."

### 2.5 Trace intent to evidence; give durable IDs
From the new schemas (IntentSpec's evidence block; ProductSpec's stable IDs):
- Where a Success criterion or Constraint has a durable source — an oracle Tier-A
  item, an ADR, an observed user friction — **cite it.** "A constraint that doesn't
  trace back is just opinion." This repo already cites Tier-A oracle items; ICS
  extends the habit.
- Give Success criteria **stable IDs** (`SC-1`, `SC-2`…) so per-phase reviews, tests,
  and PRs reference the exact criterion rather than restating it.

### 2.6 Non-functional constraints are the blind spot — force them
A 2,303-file corpus of real CLAUDE.md/AGENTS.md files skews functional (Testing 75%,
Architecture 68%) while **security and performance appear in only 14.5% each.** The
**C** surface exists precisely to force the guardrails everyone else under-provides.
If a Box has a perf budget or a security boundary, it goes in Constraints — the
ecosystem default is to forget it.

---

## 3. How it's run

### 3.1 Authoring (guided, bounded)
ICS is produced by a **guided authoring flow** ([`the-box.md`](the-box.md) §4.0;
[`PLANS/the-box-implementation.md`](../PLANS/the-box-implementation.md) S6), not a bare
file write. It's the **bounded sibling of the planning flow**: a fixed template drives
a finite Q&A (Intent → Constraints → Success), reusing the Flow/Steps engine and the
S1 decision/inbox prompts. Building it first de-risks the open-ended `IPlanner` later —
possibly one "guided authoring flow" substrate with two configs (ICS = finite;
planning = open).

Output: a committed `.box/` ICS doc that parameterizes everything downstream.

### 3.2 Anchoring the plan gate
Planning is an **open conversation**, not a swipe — the agent won't auto-plan. The ICS
is the conversation's anchor: the emitted **phased plan is derived from it**, and the
plan is presented for **approval** before any building (the cheapest place to redirect).
Approve ⇒ Box opens; deny ⇒ back into the planning conversation.

### 3.3 Anchoring ground-up review
Review flows strictly **bottom-up**; each phase's card is judged against the ICS's
**Constraints** (did this cross a line?) and **Success criteria** (does this move a
`SC-*` forward?). A deny forces a note and triggers the scoped repair
([`the-box.md`](the-box.md) §8). The ICS is what makes "approve/deny" a judgment
against a *fixed* bar rather than a vibe.

### 3.4 Amending the ICS (the soft-lock in motion)
When the work genuinely reveals the anchor is wrong (the upstream-pivot case), you
**don't edit it silently.** An amendment is proposed as a change to the `.box/` ICS and
surfaced as a **Decision** requiring the owner's hard approval — the same identity
split as any landed change (read-as-bot, approve-as-owner). Approved ⇒ the anchor moves,
consciously, and downstream plan/specs regenerate against the new version. This is the
one path by which an ICS ever changes.

```
 agent proposes ICS change ─► Decision(critical-confirm) ─► owner approves ─► anchor moves, plan regenerates
                                       │
                                       └─ deny ⇒ anchor holds; work stays inside it
```

---

## 4. How some flows work

### 4.1 Create a Box (authoring)
```
create Box ─► guided ICS Q&A ─► commit .box/ics.md ─► pick infra profile ─► ready to plan
              (Intent, then Constraints, then Success — finite prompts via Inbox)
```
The ICS and infra profile are the two creation inputs; both parameterize the run.

### 4.2 Plan against the ICS
```
ICS ─► planning conversation ─► phased plan (each PlanPhase.Intent traces to ICS Intent,
                                             each Acceptance traces to an SC-*) ─► approval gate
```
A plan whose phases don't map back to the ICS is a signal the ICS is incomplete — fix
the anchor (via §3.4), not the plan.

### 4.3 Review a phase against the ICS
```
phase build ─► card ─► human checks: any Constraint crossed?  which SC-* advanced?
                        approve (left) ─► merges into box branch
                        deny (right, +note) ─► classify ─► scoped rebuild (the-box.md §8)
```

### 4.4 Drift caught by a Constraint
A phase that quietly violates a Constraint (touches a frozen invariant, adds a
forbidden dep, blows a perf budget) is a **deny at review** with the Constraint as the
stated reason — the guardrail does its job *before* the drift lands. The Constraint
never silently relaxes to accommodate the phase; if the boundary really needs to move,
that's an ICS amendment (§3.4), not a quiet edit.

---

## 5. Examples

### 5.1 A filled ICS (this repo — the ICS authoring flow itself)

> `.box/ics.md` for a Box that builds S6.

**Intent**
- **Problem** — Box creation needs defining docs, but there's no guided way to produce
  them; authoring by hand is inconsistent and easy to skip.
- **What** — a guided, bounded Q&A flow that turns finite prompts into a valid ICS doc.
- **North Star** — creating a Box always yields a clear, one-page anchor, with no
  hand-authoring discipline required.

**Constraints** *(guardrails — do not cross)*
- Must **not** open-endedly converse — this is the *bounded* sibling of planning
  (fixed template → finite Q&A). Cite: [`the-box.md`](the-box.md) §3, S6.
- Must **not** introduce a second Flow engine — compose the existing `Domain/Flow`
  Steps + S1 decision/inbox. (R-SPINE: validators are Steps.)
- Must **not** let the agent write the ICS unreviewed — output is a human-owned
  artifact (soft-lock, §2.3).
- No new assembly wall for one consumer (YAGNI).

**Success** *(the review bar)*
- `SC-1` — a finite Q&A run produces a doc that validates against the `ics-template.md`
  shape (I/C/S all present).
- `SC-2` — the flow's Steps are recomposable (proven by reusing the substrate under a
  second config).
- `SC-3` — warning-free build + green tests + behavior verified (run it, not just
  compile).

### 5.2 A minimal ICS (a small, well-bounded Box)

> When the work is small, the anchor is small — but all three parts still appear.

**Intent** — Replace the provisional `DelayStep` stub with a real cancellation-aware
delay, so flows can pause without a busy-wait. Done = flows can await a delay that
cancels cleanly.

**Constraints** — Must not change the `IStep` contract. Must not block a thread
(async only). Label nothing "provisional" that isn't. Honor Tier-A teardown
(anti-zombie) — a cancelled delay leaves no pending timer.

**Success** — `SC-1` a delayed flow resumes after the interval; `SC-2` cancelling the
flow cancels the delay with no leaked timer (test asserts); `SC-3` the old stub is
deleted, not left alongside.

### 5.3 What a *bad* ICS looks like (anti-patterns)

| Smell | Why it's wrong | Fix |
|---|---|---|
| Constraints written as "do X, then Y, then Z" | That's a plan/procedure, not a guardrail (§2.1) | Rewrite as "must not…" boundaries; move steps to the plan |
| Two pages of detail | It's become a PRD; past the U-curve optimum (§2.2) | Cut to the durable why + boundaries + checkable bar |
| Success = "works correctly / is high quality" | Not observable; nothing to review against (§2.4) | Replace with `SC-*` acceptance checks / parity gates |
| No security/perf line on a Box that has budgets | The ecosystem blind spot (§2.6) | Add the non-functional Constraint explicitly |
| Agent edited the ICS mid-build to "match reality" | Drift leaked into the anchor (§2.3) | Revert; route through the amendment Decision (§3.4) |

---

## 6. Relationship to the rest of the repo

| Artifact | Role | ICS relationship |
|---|---|---|
| [`behavioral-oracle.md`](behavioral-oracle.md) (Tier-A) | Repo-wide constitution / durable invariants | ICS **Constraints cite it**; the oracle is the standing anchor, an ICS is the per-Box one |
| `PLANS/rebuild/02-prd.md` (EARS) | Requirements / acceptance | ICS Success maps to these as the checkable bar |
| ADRs | Durable decisions handed off as briefs | Candidate **evidence** links from ICS Intent/Constraints (§2.5) |
| [`ics-template.md`](ics-template.md) | The fill-in artifact | This doc explains it; the template is what you author |
| [`the-box.md`](the-box.md) §4.0 | Where ICS is authored + consumed | ICS is a Box creation input, committed to `.box/` |

---

## Sources

Both research passes under [`research/`](research): `intent-vs-spec-driven-development.md`
(2026-06-01 — the ladder, spec-anchored stance, TDD/NL-fixture boundary conditions) and
`authoring-locked-artifacts-2026-followup.md` (2026-07-18 — Guardrails-Beat-Guidance,
the new schemas, the U-curve, the soft-lock decision). Flow wiring: `the-box.md` §3–§4,
`PLANS/the-box-implementation.md` S6.
