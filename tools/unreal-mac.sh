#!/usr/bin/env bash
# Build the editor module, verify the campaign in UE, then cook and stage the Mac player.
set -euo pipefail
root=$(cd "$(dirname "$0")/.." && pwd)
project="$root/engines/Unreal/MosaicPier.uproject"
engine=${1:?Pass the Unreal installation directory, for example /Users/Shared/Epic\ Games/UE_5.5}
batch="$engine/Engine/Build/BatchFiles"
editor="$engine/Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor"
if [[ ! -x "$editor" || ! -f "$batch/Mac/Build.sh" || ! -f "$batch/RunUAT.sh" ]]; then
    echo "Unreal Editor, Unreal Build Tool and Automation Tool are required at $engine." >&2
    exit 2
fi
xcrun xcodebuild -version
mkdir -p "$root/build/unreal"
out=$(mktemp -d "$root/build/unreal/run.XXXXXX")
xcrun --sdk macosx metal -x metal -c /dev/null -o "$out/metal-check.air"
bash "$batch/Mac/Build.sh" MosaicPierEditor Mac Development -Project="$project" -WaitMutex
"$editor" "$project" -unattended -nop4 -nosplash -NullRHI \
    '-ExecCmds=Automation RunTests MosaicPier.Gameplay.Campaign' \
    '-TestExit=Automation Test Queue Empty' "-ReportExportPath=$out/report" "-abslog=$out/editor.log"
python3 - "$out/report/index.json" <<'PY'
import json
from pathlib import Path
import sys
with Path(sys.argv[1]).open(encoding="utf-8-sig") as file:
    report = json.load(file)
results = [test for test in report["tests"] if test["fullTestPath"] == "MosaicPier.Gameplay.Campaign"]
if len(results) != 1 or results[0]["state"] != "Success":
    raise SystemExit("The campaign test did not pass. Inspect the Unreal Automation report.")
PY
bash "$batch/RunUAT.sh" BuildCookRun -project="$project" -platform=Mac -clientconfig=Development \
    -build -cook -map=/Engine/Maps/Entry -stage -pak -package -archive -archivedirectory="$out/player" \
    -nop4 -unattended -utf8output
app=$(find "$out/player" -type d -name 'MosaicPier.app' -print -quit)
if [[ -z "$app" || ! -x "$app/Contents/MacOS/MosaicPier" ]]; then
    echo "Packaging returned without a Mosaic Pier executable." >&2
    exit 1
fi
printf 'Player: %s\nAutomation: %s\n' "$app" "$out/report/index.json"
