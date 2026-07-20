# Authoring Hard-Locked Up-Front Artifacts (ICS) — 2026 Follow-Up

> Deep-research synthesis (fan-out web search → 3-vote adversarial verification →
> cited synthesis). Produced 2026-07-18. **Follow-up** to
> [`intent-vs-spec-driven-development.md`](intent-vs-spec-driven-development.md)
> (2026-06-01): that report mapped the intent-vs-spec debate, the convergent
> Objective→Outcomes→Constraints→Verification shape, tooling (Spec Kit, Kiro,
> Tessl, BMAD), and the TDD/NL-fixtures analogy. This pass hunts for what is **NEW**
> since ~April 2026 on the specific practice behind our [`ics-template.md`](../ics-template.md):
> authoring durable, hard-locked artifacts (**I**ntent · **C**onstraints · **S**uccess)
> up front and letting the agent work inside them.
>
> Confidence tags: **[H]** = strong/primary + multiple angles, **[M]** = single
> decent source or thin/indirect empirical support, **[L]** = vendor/marketing.
> 25 claims verified (3 skeptics each, 2/3 refutes kills): **19 confirmed, 6 refuted.**

---

## TL;DR

- **Your instinct now has a real empirical anchor.** A 5,000-run controlled study
  on SWE-bench Verified — **"Guardrails Beat Guidance"** (arXiv 2604.11088, Opus 4.6)
  — found **every individually *beneficial* rule was a negative constraint ("do not X")
  and every individually *harmful* one a positive directive ("do X")**. This is the
  single strongest new result for *constraints-beat-guidance*, and it says an ICS
  template should **foreground the C** (negative constraints), not prose how-tos. **[H]**
- **A caveat in the same study cuts the other way:** merely *having* a persistent
  config file drove most of the gain — **random rules matched curated rules at an
  identical 63.8%** pass rate (vs 50.0% no-rule baseline). Presence may matter more
  than tight authoring. This is the sharpest open question for us. **[H]**
- **The artifact format crystallized into named schemas** in this window: Gokul
  Rajaram's **ProductSpec** ("Product Harness"), Pathmode's **IntentSpec**
  (CI-validated, adds an **evidence/traceability** block), and academic
  **Spec Growth Engine** (frozen "Layer-1 invariants" + Contract/Design split).
  All three are directly usable to sharpen our one-page ICS. **[H]/[M]**
- **The discourse shifted to "specification is the bottleneck, not implementation."**
  O'Reilly's Markus Eisele and Addy Osmani both argue vague specs don't remove
  cost — they **defer and fragment it** downstream. But Eisele's curve is **U-shaped**:
  the optimum is *well-structured acceptance criteria in the middle*, **not** maximalist
  hard-locking. A caution against over-authoring. **[M]**
- **"Hard-locking" is mostly aspirational.** In practice these artifacts are enforced
  as **advisory guardrails + CI drift-gates / JSON-Schema validation**, not literal
  immutability. Claims that machine-enforced *blocking-merge-on-drift* is the real
  novelty were **refuted**. **Our stance (§6.7): soft-lock** — no auto-mutation, every
  artifact change gated on human review + hard approval; the value is anchor integrity
  over the project's life, not per-task output quality. **[H]**
- **Non-functional constraints are the field's blind spot.** A 2,303-file corpus of
  real CLAUDE.md/AGENTS.md shows context files skew functional (Testing 75%,
  Architecture 68%) while **security and performance appear in only 14.5% each** —
  exactly the "C" surface an ICS is meant to force. **[H]**

---

## 1. The empirical core — "Guardrails Beat Guidance" (the headline finding)

**arXiv 2604.11088** · Opus 4.6 · 5,000+ Claude Code runs on SWE-bench Verified ·
679 rule files / 25,532 rules · **[H]** (3-0 verified)

| Finding | Number | What it means for ICS |
|---|---|---|
| Negative constraints help, positive directives hurt | *Every* beneficial rule was "do not X"; *every* harmful one was "do X" | Write Constraints as **prohibitions/boundaries**, not procedures |
| Presence > content | Random rules == curated rules at **63.8%** pass (vs **50.0%** no-rule baseline, **+13.8pp**) | A "context priming" effect — *having* a locked artifact matters, maybe more than its precise wording |

**Read carefully.** The polarity split is real but rests on a per-rule ablation of
only **~18 curated rules** (small clean sample), and it's Opus-4.6-specific on a
bug-fixing benchmark. The paper itself acknowledges "secondary sensitivity to
content." Directionally strong, quantitatively young.

**The uncomfortable implication for us:** if *presence* drives most of the gain, the
marginal value of *tightly* authoring an ICS (vs having any durable artifact) is
unproven. Our own experience says tight authoring helps — but we don't yet have a
head-to-head that isolates it. See Open Questions.

---

## 2. The new artifact schemas (sharpen `ics-template.md` against these)

Three named, concrete formats matured in-window. Field-level, and each contributes
something our current one-page template lacks.

### ProductSpec — Gokul Rajaram (v0.23.0, 2026-07-13) · **[H]** (3-0)
An open "**Product Harness**": *what to build, what NOT to build, how to prove
completion, and when intent changes.* Six mandatory sections:

`Problem · Hypothesis · Product Summary · Scope (in / out / cut) · Acceptance Criteria · Success Metrics`
(+ optional AI eval sections).

- **Borrow: the explicit `Scope: in / out / cut` triad.** Each item is "a
  sentence-level guardrail that can stand alone in an agent plan or PR." Our
  Constraints section names non-goals; ProductSpec makes *out* and *cut* first-class,
  distinct fields.
- **Borrow: durable stable IDs** — `AC-1`, `SM-1`, `EVAL-1` — that evidence artifacts
  (PRs, tests, eval runs) attach back to. This is the traceability spine.
- ⚠️ Brand-new, one-author, ~60 stars; "standard" is self-declared. Enforcement is
  advisory (agents told to cite/respect), not mechanical.

### IntentSpec — Pathmode, maintainer Janne Lammi (intentspec.org) · **[H]** (3-0)
Markdown + YAML frontmatter, validated against a **JSON Schema via ajv + a GitHub
Action**. Fields: `id, status, objective, evidence, outcomes, constraints, edgeCases,
healthMetrics, verification, scope`.

- **Borrow: the `evidence` block** — the genuinely new field vs the prior report's
  shape. Each outcome/edge case must trace to a source item (`type: quote|friction,
  source, excerpt, anchors`). *"A spec that doesn't trace back to evidence is just
  opinion."* This tells the agent **why** each outcome exists, not just what.
- **Borrow: CI-validated schema.** The lock isn't immutability — it's a schema the
  build enforces. Fits our "specs regenerable but gated" stance.
- ⚠️ Vendor site; required-vs-optional field status unconfirmed; no empirical perf claim.

### Spec Growth Engine — Hartwig Grabowski (arXiv 2606.27045, Jun 2026) · **[H]** (3-0)
Closest academic analog to *our* layered build. One `SPEC.md` per node, split into:
- **Contract** (outward): public interfaces, invariants, types, error behaviour, acceptance criteria
- **Design** (inward): implementation reasoning

**Layer-1 invariants** — root invariants + container boundaries (persistence,
security, external integrations, error taxonomy) — are **authored up front and
frozen before any feature**, forming *"the floor below which the architecture cannot
silently erode."* Everything else grows just-in-time.

- **This maps almost 1:1 onto our oracle + layered plan:** frozen Layer-1 invariants
  ≈ Tier-A oracle; just-in-time growth ≈ walking-skeleton L1→L12.
- ⚠️ "Frozen" is a **drift gate governing change, not literal immutability.** The
  claim that blocking-merge-on-drift is the central novelty was **refuted (1-2)** —
  cite the Contract/Design split and the frozen-invariants idea, *not* an enforcement
  mechanism.

### Two more field-tested schemas (blog-quality, for cross-reference)
- **Addy Osmani** ("How to write a good spec for AI agents," Jan 2026): six areas —
  `Commands · Testing · Project Structure · Code Style · Git Workflow · Boundaries`,
  where **Boundaries is a three-tier `Always / Ask-first / Never`** and *"Never commit
  secrets"* is the single most common helpful constraint. The tiered-boundary idea is
  a clean upgrade for our Constraints surface. **[M]**
- **Augment Code** ("AI spec template," updated Jun 2026): 7 sections incl.
  `Business Rules/Constraints`, `Boundaries/Guardrails`, `Test Plan/Self-Verification`. **[L]**

---

## 3. The discourse shift — "specification is the bottleneck"

A convergent 2026 thread reframes the problem away from code generation. **[M]**

- **Markus Eisele** (O'Reilly Radar, "Why AI Coding Agents Still Need Clear Specs,"
  2026-07-08): the hard part is *"agreeing what should exist, what should never
  happen, which trade-offs matter."* Agent speed **exposes** under-specification — a
  plausible implementation appears before anyone decided what it should mean. Vague
  specs *"don't eliminate cost. You're deferring it, fragmenting it, and making it
  harder to see."*
  - **Critical nuance for us:** Eisele's cost curve is **U-shaped**. The optimum is
    *well-structured acceptance criteria / BDD scenarios in the middle* — **not**
    unbounded hard-locking. This is the strongest published caution against
    over-authoring an ICS. Keep it one page *on purpose*.
- **Pathmode** ("The Orchestration Era Needs Intent"): frames an intent spec as
  *"a durable, executable artifact"* — the artifact-first framing, vendor-side. **[L]**

**Constitutions / durable context** (connects ICS to AGENTS.md-style files):
- The **"project constitution as CLAUDE.md"** pattern — loaded automatically before
  any prompt/spec/task — is now explicitly named as durable, locked context separate
  from feature specs. This is exactly the role our oracle + CLAUDE.md already play. **[M]**
- **Constitutional Spec-Driven Development** (arXiv 2602.02584): author a versioned,
  **machine-readable Constitution** encoding security constraints from **CWE / MITRE
  Top 25** so output complies *"by construction rather than inspection."* **Cite the
  method, not the number** — its 73% defect-reduction figure was **refuted (0-3)** as
  a single n=1 case study. **[M]**

---

## 4. Non-functional constraints are the field's blind spot

**"Agent READMEs"** (arXiv 2511.12884) — 2,303 real context files across 1,925 repos
(CLAUDE.md / AGENTS.md / copilot-instructions.md). **[H]** (3-0)

| Category | % of files | |
|---|---|---|
| Testing | 75.0% | ← functional guidance dominates |
| Implementation Details | 69.9% | |
| Architecture | 67.7% | |
| Development Process | 63.3% | |
| Build and Run | 62.3% | |
| **Security** | **14.5%** | ← the "C" surface is nearly absent |
| **Performance** | **14.5%** | |

Files are *"heavily skewed toward functional operations"* while non-functional
requirements are *"rarely specified."* Structurally they're shallow (single H1,
median 6–7 H2s). **The takeaway: an ICS that forces an explicit Constraints surface
is filling the exact gap the ecosystem systematically under-provides.** (Nov 2025,
just before the priority window, but directly on-point.)

Supporting signal: an **ETH Zurich study** (via Augment) found **human-curated
context files beat LLM-generated ones across all four agents tested (~4pp)** —
evidence that deliberate authoring beats auto-generation, even if tightness-of-lock
is unproven. **[M]**

---

## 5. What got refuted (don't cite these)

The adversarial pass killed 6 of 25 claims — recording them so we don't accidentally
lean on them later:

| Refuted claim | Vote | Why |
|---|---|---|
| Constitutional SDD "73% fewer security defects" | 0-3 | Single n=1 banking-app case study; cite the *method*, not the figure |
| Spec Growth Engine's novelty = blocking-merge-on-drift enforcement | 1-2 | Enforcement is a drift *gate*, not structural impossibility |
| `agent-guardrails-template` uses exactly the ICS shape | 0-3 | Repo doesn't match the claimed Intent/Constraints/Success structure |
| That template "locks" guardrails via 17-tool MCP server + REST gate | 1-2 | Unsupported by the source |
| Generation-time constraints *measurably* beat review-time (via the 73% figure) | 0-3 | Same weak n=1 basis |
| Corpus study "50 agent loops, 74% name terminal states" as ICS-success evidence | 1-2 | Source didn't support the framing/numbers |

---

## 6. Implications for our ICS template + practice

Concrete, lowest-risk → highest-leverage:

1. **Lead the Constraints section with negative constraints** ("must not," boundaries,
   forbidden deps) — the *only* rule polarity the Guardrails study found beneficial.
   Demote prescriptive "do X" procedure. *(§Constraints of `ics-template.md`.)*
2. **Split Scope into `in / out / cut`** (ProductSpec) instead of a single non-goals
   bullet. `out` = deliberately excluded; `cut` = considered-and-dropped. Cheap,
   high-signal for an agent.
3. **Add a tiered boundary line** (Osmani): `Always / Ask-first / Never`. Slots
   directly under Constraints; "Never commit secrets"–class rules live here.
4. **Add stable IDs to Success criteria** (`SC-1`…) so per-phase reviews and tests
   cite the exact criterion — we already cite Tier-A oracle items; extend the habit
   to the ICS's own success bullets.
5. **Consider a light `evidence` pointer** (IntentSpec) on Intent/Success bullets —
   link the durable "why" (oracle item, ADR, user friction) so the agent gets
   rationale, not just assertion. Keep it a pointer, not a transcript.
6. **Keep it one page — deliberately.** Eisele's U-curve is the counterweight to our
   own "we saw better work when we locked things down" instinct: the optimum is
   *structured but bounded*, and over-authoring re-introduces the brittleness the
   prior report warned about. Our existing "a Box's ICS is not a PRD" line is correct;
   this research *reinforces* it rather than pushing toward maximalism.
7. **Soft-lock the ICS — this is our decision, and it reframes the "freezing"
   question.** The research measured *per-task output quality*, where freezing may
   not beat mere presence. But that is the wrong axis. Freezing buys **anchor
   integrity over the project's life** and **lower mental load** — you need to know
   your anchor is safe and clear. The failure mode it prevents is the important one:
   if the artifact can mutate freely, accumulated drift **leaks back into the anchor
   itself**, and you steer the wrong way while still trusting it. A
   *present-but-corrupted* anchor is worse than a stable one — "presence beats
   content" only holds while the present thing stays true. So:
   - **No auto-mutation.** The agent never edits the ICS as a side effect of shipping.
   - **Every change requires human review + hard approval** — a conscious, logged
     decision that we are moving a guardrail, never the first-resort move.
   - **Default is work-within-the-anchor**, not amend-the-anchor.

   This is not a new mechanism: it is exactly the **protected-paths + required-PR-review**
   soft-lock the repo already applies to its enforcement surface (ADRs, harness, CI).
   The ICS inherits that governance rather than inventing one.

---

## Open questions (what this pass could NOT settle)

1. **Does *freezing* an authored artifact beat letting it evolve mid-task — or does
   only *presence* matter?** The 63.8% random==curated result suggests presence may
   dominate content *on per-task output quality*. But that axis misframes the
   question (see §6.7): freezing's payoff is **anchor integrity over the project's
   life** — preventing drift from silently corrupting the artifact you steer by —
   which no study here measures. Our resolution is not to freeze harder for output
   quality, but to **soft-lock** for drift-integrity: no auto-mutation, human-approved
   changes only. The open empirical question narrows to: *how much does an unfrozen
   anchor degrade over a long project, and does soft-lock measurably reduce that
   drift?*
2. **Optimal ICS granularity/length?** Eisele implies a sweet spot but nobody
   quantifies where over-specification starts hurting.
3. **Are ProductSpec / IntentSpec / SPEC.md converging or fragmenting?** No consortium
   or shared standard yet — all single-steward.
4. **Does negative>positive generalize** beyond Opus 4.6 / SWE-bench to greenfield
   feature work? And does IntentSpec's `evidence` block measurably change behavior?

---

## Sources

**Empirical (primary):** arxiv.org/html/2604.11088 (Guardrails Beat Guidance);
arxiv.org/html/2511.12884v1 (Agent READMEs); arxiv.org/pdf/2606.27045 (Spec Growth
Engine); arxiv.org/abs/2602.02584 (Constitutional SDD); arxiv.org/abs/2607.02389
(Steerability via constraints, ICML 2026 DL4C workshop).

**Artifact schemas:** github.com/gokulrajaram/ProductSpec; intentspec.org +
pathmode.io; addyosmani.com/blog/good-spec; augmentcode.com/guides/ai-spec-template.

**Discourse / constitutions:** oreilly.com/radar/why-ai-coding-agents-still-need-clear-specs
(Eisele); pathmode.io/blog/orchestration-era-needs-intent;
agentfactory.panaversity.org (project constitution / CLAUDE.md);
martinfowler.com/articles/exploring-gen-ai/context-engineering-coding-agents.html;
productcompass.pm/p/intent-engineering-framework-for-ai-agents;
bitbytebit.substack.com/p/agent-hooks-deterministic-guardrails.

**Method:** deep-research harness — 5 angles, 21 sources fetched, 102 claims
extracted, 25 verified (3-vote adversarial, 2/3 to kill), 19 confirmed / 6 refuted,
synthesized 2026-07-18.
