#!/usr/bin/env bash
#
# Makes sure changelog.md has a section for the version being released.
#
# Two ways to write release notes are supported:
#
#   1. Write them straight under a "## <version>" heading (as for 3.0.0). The
#      heading already exists, so there is nothing to roll and the file is left
#      unchanged.
#
#   2. Collect them under "## Unreleased". That section is rolled into a version
#      heading, and a fresh, empty "## Unreleased" is left at the top:
#
#        ## Unreleased          ->   ## Unreleased
#
#        ### Fixes                   ## 3.0.1
#        - ...
#                                    ### Fixes
#                                    - ...
#
# Usage: roll-changelog.sh <version> [changelog-path]
#
#   version         Version being released, e.g. 3.0.1 (a leading "v" is dropped).
#   changelog-path  Defaults to changelog.md in the repository root.
#
# Exits non-zero if neither form is present — releasing with nothing written down
# is almost always a mistake — or if both are, because the Unreleased notes would
# then be left out of the release.

set -euo pipefail

version=${1:?usage: roll-changelog.sh <version> [changelog-path]}
changelog=${2:-"$(dirname "$0")/../../changelog.md"}

version=${version#v}
heading="## ${version}"
version_pattern="^## ${version//./\\.}[[:space:]]*\$"

if [[ ! -f $changelog ]]; then
  echo "::error::Changelog not found: $changelog" >&2
  exit 2
fi

# Everything between "## Unreleased" and the next "## " heading (or EOF).
unreleased_body=$(awk '
  /^## Unreleased[[:space:]]*$/ { inside = 1; next }
  inside && /^## / { exit }
  inside { print }
' "$changelog")
has_unreleased=false
[[ -n ${unreleased_body//[[:space:]]/} ]] && has_unreleased=true

if grep -qE "$version_pattern" "$changelog"; then
  if $has_unreleased; then
    echo "::error::$changelog has both a '$heading' section and Unreleased notes — move the Unreleased notes into '$heading'" >&2
    exit 3
  fi
  echo "$changelog already has a '$heading' section — nothing to roll"
  exit 0
fi

if ! $has_unreleased; then
  echo "::error::$changelog has no '$heading' section and no Unreleased notes — nothing to release" >&2
  exit 4
fi

tmp=$(mktemp)
trap 'rm -f "$tmp"' EXIT

awk -v heading="$heading" '
  { print }
  !done && /^## Unreleased[[:space:]]*$/ {
    print ""
    print heading
    done = 1
  }
' "$changelog" >"$tmp"

mv "$tmp" "$changelog"
trap - EXIT

echo "Rolled Unreleased into '${heading}' in ${changelog}"
