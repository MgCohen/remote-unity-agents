---
docType: rulebook
testType: unit
rubric: ../../../../tests/Rubrics/Unit.md
harness: ../../../../tests/Harness/README.md
---

## Rules

### DocValidator.Validate → no errors for a catalog-conforming document
<!-- id: docvalidator-validate-no-errors-for-a-catalog-conforming-document -->
- **Why:** the validator's contract is that a document matching its doctype's blocks, required attrs, and labels
  passes clean — so a real, build-enforced instance must return an empty error list, proving the happy path is
  not accidentally rejecting valid docs.

### DocValidator.Validate → flags a front-matter enum value outside the doctype's allowed set
<!-- id: docvalidator-validate-flags-a-front-matter-enum-value-outside-the-doctype-s-allowed-set -->
- **Why:** a doctype that constrains a front-matter attr to an enum (e.g. `testType`) must reject a value off
  that list; this is the reject path the shell-out happy-path test never exercises, so a regression that stopped
  enforcing enums would otherwise pass silently.

### DocValidator.Validate → flags a missing required front-matter attribute
<!-- id: docvalidator-validate-flags-a-missing-required-front-matter-attribute -->
- **Why:** required front-matter attrs are a hard floor — dropping one must produce an error naming it, so a doc
  cannot ship missing the metadata its doctype declares mandatory.

### DocValidator.Validate → no errors for a guide whose procedures nest conforming steps
<!-- id: docvalidator-validate-no-errors-for-a-guide-whose-procedures-nest-conforming-steps -->
- **Why:** nested-block composition (`composes`) is the engine's third structural level — a procedure holding
  `##### N. step` children. A guide with well-formed nested steps must validate clean, proving the recursive parse and
  validate accept the happy path rather than rejecting any nesting outright. The fixture brackets its steps with
  procedure labels (Context before, Outcome after), so a passing run also proves those bare lead-in labels route to the
  procedure, not the trailing step.

### DocValidator.Validate → an ancestor's label after a nested child attaches to the ancestor
<!-- id: docvalidator-validate-an-ancestor-s-label-after-a-nested-child-attaches-to-the-ancestor -->
- **Why:** a label belongs to whichever block in the nesting chain declares it, regardless of position — so
  a procedure's Outcome may sit *after* its steps. Dropping the trailing Outcome must surface as the
  **procedure** missing it (not the step swallowing it), proving the label routed up to its declaring ancestor.

### DocValidator.Validate → flags a step id that violates its attr pattern
<!-- id: docvalidator-validate-flags-a-step-id-that-violates-its-attr-pattern -->
- **Why:** a step's `id` is the leading ordinal of its `##### ` heading, a `pattern`-enforced attr; an id off its
  grammar (e.g. `1.X`) must fail, so the `pattern` validator is proven to run on real attrs and the visible step
  number cannot drift out of its format.

### DocValidator.Validate → flags a step ordinal written with a non-canonical separator
<!-- id: docvalidator-validate-flags-a-step-ordinal-written-with-a-non-canonical-separator -->
- **Why:** the ordinal split strips only a trailing `.`, so `##### 1) First step` parses the id as `1)` and fails the
  `pattern` loudly rather than being silently accepted as `1` — keeping one canonical `N.` form so authors do not drift.

### DocValidator.Validate → a bare **Label:** lead-in whose name is undeclared stays prose, not an unexpected label
<!-- id: docvalidator-validate-a-bare-label-lead-in-whose-name-is-undeclared-stays-prose-not-an-unexpected-label -->
- **Why:** a label may be a `- **Name:**` bullet or a bare `**Name:**` lead-in, but the bare form is only a label when
  its name is declared — so ordinary bold-lead prose (`**Note:** …`) inside a body is not mistaken for an unexpected
  label, while a declared bare label (Context/Outcome) still routes and validates.

### DocValidator.Validate → flags duplicate step ids within one procedure
<!-- id: docvalidator-validate-flags-duplicate-step-ids-within-one-procedure -->
- **Why:** step ids are unique within their procedure (siblings), the handle a cross-reference resolves against;
  two steps sharing an id must error, while the same id reused in a different procedure stays legal.

### DocValidator.Validate → flags a block that composes a child type but has no child
<!-- id: docvalidator-validate-flags-a-block-that-composes-a-child-type-but-has-no-child -->
- **Why:** a block that declares `composes` requires at least one such child — a procedure with zero steps is an
  empty how-to and must fail; this per-parent required-child rule is distinct from the group-emptiness rule and
  must hold at each composed level.

### DocValidator.Validate → flags an onChange path outside the allowlisted roots
<!-- id: docvalidator-validate-flags-an-onchange-path-outside-the-allowlisted-roots -->
- **Why:** `onChange` is a universal optional handler any doc may declare, and the engine validates the pointer
  (not its execution): a path that escapes the runnable roots (`.claude/agents`, `.claude/hooks`, `scripts/`) or
  contains `..` must fail, so a doc can never aim its change-handler at an arbitrary executable.

### DocValidator.Warnings → flags an undeclared front-matter key without failing validation
<!-- id: docvalidator-warnings-flags-an-undeclared-front-matter-key-without-failing-validation -->
- **Why:** `Validate` silently ignores front-matter keys the doctype never declared, which let the authoring
  procedure and the doctype drift apart unnoticed (a `source` key no doctype declares); the advisory tier must
  name the undeclared key while validation itself stays green, so drift surfaces without blocking a valid doc.

### DocValidator.Warnings → declared attrs and the universal keys warn nothing
<!-- id: docvalidator-warnings-declared-attrs-and-the-universal-keys-warn-nothing -->
- **Why:** the warning is for drift only — a declared attr (`status`) and the universal keys every doc may carry
  (`docType`, `onChange`) are legitimate, so warning on them would train authors to ignore the channel.

### Grading.Sections → the doc section first, then one focused section per block type present
<!-- id: grading-sections-the-doc-section-first-then-one-focused-section-per-block-type-present -->
- **Why:** block rubrics guided authoring but gated nothing — the judge graded only the doctype rubric, so a
  block violating its own rules could still grade clean. The fix is a grading *plan*, not one concatenated
  rubric: each section is a single-context judge call (the doc against document-level criteria, each present
  block type against its own, with its members named), so every block rubric is enforced without handing one
  judge a blob that blurs doc-level and block-level concerns.

### Grading.Sections → a block type absent from the instance contributes no section
<!-- id: grading-sections-a-block-type-absent-from-the-instance-contributes-no-section -->
- **Why:** grading a document against rules for blocks it does not contain invites vacuous or indeterminate
  verdicts; only the block types actually present earn a section, keeping every judge call judgeable on the
  artifact itself.

### Grading.Sections → nested children's block types get their own section
<!-- id: grading-sections-nested-children-s-block-types-get-their-own-section -->
- **Why:** composed children (a procedure's steps) are blocks like any other and carry their own rubrics; if
  presence were computed only at the top level, their standards would silently drop out of the grading plan.

### SchemaChecker.Run → no errors for the shipped catalog
<!-- id: schemachecker-run-no-errors-for-the-shipped-catalog -->
- **Why:** the catalog the whole repo validates against must itself conform to the meta-schema; a non-vacuous
  pass over the real `_schema`/`kinds`/`blocks`/`doctypes` proves the checker does real work and the catalog is sound.

### Reviewers.Resolve → defaults to the judge for a docType that declares none
<!-- id: reviewers-resolve-defaults-to-the-judge-for-a-doctype-that-declares-none -->
- **Why:** every doc is graded against its rubric by the shared judge — that universal must hold with no
  per-docType config, so a docType that names no `reviewers:` still resolves to `[judge]`, never to nothing.

### Reviewers.Resolve → returns the docType's declared reviewers when present
<!-- id: reviewers-resolve-returns-the-doctype-s-declared-reviewers-when-present -->
- **Why:** a docType opts into extra fresh reviewers (the guide adds `walk-guide`) via its `reviewers:` field, so
  when present the resolver must return exactly that list — the seam that lets one docType react differently.

### Checks.Resolve → empty for a docType with no custom deterministic checks
<!-- id: checks-resolve-empty-for-a-doctype-with-no-custom-deterministic-checks -->
- **Why:** custom deterministic checks are strictly opt-in — the generic `validate` is the universal floor, so a
  docType that declares no `checks:` must resolve to none, never inventing a blocking rule it didn't ask for.

### Checks.Resolve → returns the docType's declared checks when present
<!-- id: checks-resolve-returns-the-doctype-s-declared-checks-when-present -->
- **Why:** a docType names its own cheap, objective guards via `checks:`; when present the resolver must return
  exactly that list so the handler can run each as a blocking gate the generic structural validator can't express.

### SchemaChecker.Run → flags a definition file that is not a YAML map
<!-- id: schemachecker-run-flags-a-definition-file-that-is-not-a-yaml-map -->
- **Why:** a definition that is not a YAML map is structurally broken; the checker must report it rather than
  skip or throw, so a corrupted block/doctype can never silently weaken the standard.

### SchemaChecker.Run → fails loud when a catalog definition directory is missing
<!-- id: schemachecker-run-fails-loud-when-a-catalog-definition-directory-is-missing -->
- **Why:** a renamed or emptied `kinds`/`blocks`/`doctypes` directory makes the checker validate zero
  definitions and return PASS — a vacuous green. The checker must report the missing collection so a broken
  catalog layout can never look sound.

### SchemaChecker.Run → flags a composes entry that names no block type
<!-- id: schemachecker-run-flags-a-composes-entry-that-names-no-block-type -->
- **Why:** `composes` is referential — every entry must name a real block definition. A typo'd or dangling
  child type must fail `check`, so nesting can never point at a block that does not exist.

### DocValidator.Validate → flags a block with no explicit id handle
<!-- id: docvalidator-validate-flags-a-block-with-no-explicit-id-handle -->
- **Why:** every block must be addressable by a stable id so a client (or a cross-reference) can name exactly
  one block; a block with no `<!-- id: … -->` handle must fail, pointing the author at `ids --write` — the
  requirement that makes ids genuinely mandatory rather than optional.

### Ids.Stamp → fills every block missing an id, and a stamped doc then validates
<!-- id: ids-stamp-fills-every-block-missing-an-id-and-a-stamped-doc-then-validates -->
- **Why:** the stamp is how the mandatory-id rule is satisfied without hand-typing — every singleton and member
  lacking a handle gets one, so a freshly authored doc goes from failing require-id to validating in one pass.

### Ids.Stamp → is idempotent: a second pass changes nothing
<!-- id: ids-stamp-is-idempotent-a-second-pass-changes-nothing -->
- **Why:** stamping runs in the authoring loop and on every re-edit; a second pass must leave existing handles
  untouched, or ids would churn and the client references keyed to them would break.

### Ids.Stamp → a derived-id collision gets a numeric suffix
<!-- id: ids-stamp-a-derived-id-collision-gets-a-numeric-suffix -->
- **Why:** two members deriving the same slug (identical titles) must still get distinct ids, so the uniqueness
  the addressing depends on holds automatically rather than forcing the author to resolve every clash by hand.

### Ids.Stamp → an existing id survives a retitle, so a reference never breaks
<!-- id: ids-stamp-an-existing-id-survives-a-retitle-so-a-reference-never-breaks -->
- **Why:** an id is frozen once written — rewording a block's title must not regenerate its handle, because a
  stable id that silently changes on an edit is worse than none: the client's stored reference would dangle.

### Ids.Stamp → derives an id-safe slug from a title carrying punctuation
<!-- id: ids-stamp-derives-an-id-safe-slug-from-a-title-carrying-punctuation -->
- **Why:** a block's title can hold arbitrary punctuation (a rule header with `→`, `.`, `!`), but its id must be
  a bare `[a-z0-9-]` handle the parser reads back and a client can carry; the slug must strip, not embed, that punctuation.
