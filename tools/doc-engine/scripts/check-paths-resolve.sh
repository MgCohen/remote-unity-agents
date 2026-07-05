#!/bin/sh
# Blocking per-docType check ($1 = the changed doc, run from the repo root): every backtick-cited
# repo path must resolve on disk — an invented path is the cheapest factual error to catch
# mechanically. Only dir-prefixed tokens under the repo's known top-level roots are checked, so
# symbols, globs, and endpoint routes never false-positive. A path the doc plans to create is
# exempted by writing "(planned)" anywhere on the same line — the failure message teaches this.
set -eu

doc="$1"
root=$(git rev-parse --show-toplevel 2>/dev/null || pwd)
missing=""
lineno=0

while IFS= read -r line || [ -n "$line" ]; do
    lineno=$((lineno + 1))
    case "$line" in *"(planned)"*) continue ;; esac
    tokens=$(printf '%s\n' "$line" | grep -o '`[^`]*`' | tr -d '`' | tr ' ' '\n') || continue
    for token in $tokens; do
        case "$token" in
            *"*"* | *"?"* | *"{"* | *"<"*) continue ;;
            src/* | tests/* | tools/* | design/* | PLANS/* | governance/* | scripts/* | prototype/* | .claude/* | .github/* | .githooks/*) ;;
            *) continue ;;
        esac
        path=${token%%:*}
        path=${path%/}
        [ -e "$root/$path" ] || missing="$missing
  $doc:$lineno: cited path '$token' does not resolve — fix it, or mark the line '(planned)' if it is yet to be created"
    done
done < "$doc"

[ -z "$missing" ] || { printf 'unresolved cited paths:%s\n' "$missing"; exit 1; }
exit 0
