# ICS — Box authoring template

> **Status:** Minimal fill-in template for [`the-box.md`](the-box.md) §4.0 (Box
> creation). The *why and how* behind it — principles, flows, examples — is
> [`ics.md`](ics.md); read that once, then author from this. **Minimal on purpose**
> (the U-curve optimum is in the middle) — extend on the second real need, not the first.

**ICS = Intent · Constraints · Success.** Every Box is authored with these three, written
*before* planning. They anchor the planning conversation (`IPlanner`) and the ground-up
review: the plan is derived from the ICS, and each phase's review checks code against it.
Keep it to a page — a Box's ICS is not a PRD.

> **Soft-locked.** No auto-mutation; the agent never edits this as a side effect of
> shipping. Any change is a human-approved Decision (`ics.md` §3.4) — moving a guardrail
> is deliberate, never the first-resort move.

## Intent  *(the durable why / what)*

- **Problem / motivation** — why this stream of work exists.
- **What** — the change in behavior or capability, in a sentence or two.
- **North Star** — the outcome that, if reached, means this Box succeeded.

## Constraints  *(the guardrails — "don't cross these lines")*

- **Lead with the negatives** — what must **not** change; behavior/compatibility to
  preserve; forbidden deps; non-goals. (Negative constraints are the rules that help;
  procedure belongs in the plan, not here.)
- **Scope: in / out / cut** — deliberately included · deliberately excluded · considered
  and dropped. Each item stands alone as a guardrail.
- **Invariants** this Box must honor — **cite them** (Tier-A oracle items, ADRs).
- **Non-functional budgets** — security / performance boundaries (easy to forget; put
  them here if they exist).
- **Infra/workspace** constraints (the §11 profile chosen at creation).

## Success criteria  *(verification — how we'll know)*

- Observable, checkable outcomes with **stable IDs** (`SC-1`, `SC-2`…) so phase reviews,
  tests, and PRs cite the exact criterion.
- Acceptance checks / tests / parity gates — the bar each phase's ground-up review is
  judged against.
- Where a criterion has a durable source, link the **evidence** (oracle item, ADR,
  observed friction).
