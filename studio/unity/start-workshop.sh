#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/../.." && pwd)
mkdir -p "$root/build"
stage=$(mktemp -d "$root/build/unity-workshop.XXXXXX")
for directory in Assets Packages ProjectSettings; do
    cp -R "$root/engines/Unity/$directory" "$stage/"
done
cp "$root/studio/unity/PierWorkshop.cs" "$stage/Assets/Editor/"
python3 - "$stage/Packages/manifest.json" <<'PY'
import json
import sys
from pathlib import Path
path = Path(sys.argv[1])
manifest = json.loads(path.read_text())
manifest["dependencies"]["com.unity.pipeline"] = "1.0.1"
path.write_text(json.dumps(manifest, indent=2) + "\n")
PY
unity --non-interactive open "$stage" --editor-version 6000.6.10f1 \
    --accept-upgrades --batchmode --until-ready
printf 'Workshop project: %s\n' "$stage"
