#!/usr/bin/env bash
# Include the display typeface's license alongside the signed application.
set -euo pipefail
app=${1:?Pass the built Mosaic Pier.app}
out=${2:?Pass the output .zip path}
root=$(cd "$(dirname "$0")/.." && pwd)
test -x "$app/Contents/MacOS/Mosaic Pier"
codesign --verify --deep --strict "$app"
stage=$(mktemp -d "${TMPDIR:-/tmp}/mosaic-package.XXXXXX")
trap 'rm -r "$stage"' EXIT
ditto "$app" "$stage/Mosaic Pier.app"
cp "$root/web/assets/OFL.txt" "$stage/Cormorant-Garamond-LICENSE.txt"
ditto -c -k --sequesterRsrc "$stage" "$out"
