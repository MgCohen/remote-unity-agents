#!/bin/sh
# Blocking per-docType check ($1 = the changed doc, run from the repo root): a doc that carries a
# generated INDEX region must have it regenerated after edits — a stale outline misstates the doc's
# own structure. Docs without INDEX markers are exempt (the outline is optional).
set -eu

doc="$1"
grep -q 'INDEX:START' "$doc" || exit 0

engine="tools/doc-engine"
dll=$(ls "artifacts/bin/ABox.DocEngine"/*/docengine.dll 2>/dev/null | head -n 1 || true)
if [ -n "$dll" ]; then de() { dotnet "$dll" "$@"; }; else de() { dotnet run --project "$engine" -- "$@"; }; fi

tmp=$(mktemp)
trap 'rm -f "$tmp"' EXIT
cp "$doc" "$tmp"
de outline "$tmp" --write --root "$engine" >/dev/null 2>&1

if ! diff -q "$doc" "$tmp" >/dev/null 2>&1; then
    echo "$doc: INDEX is stale — run 'docengine outline $doc --write'"
    exit 1
fi
exit 0
