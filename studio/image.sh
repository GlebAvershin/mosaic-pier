#!/usr/bin/env bash
set -euo pipefail
platform=$1
game=$2
stage=$(mktemp -d)
trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/platform" "$stage/game/core" "$stage/game/tools"
cp "$platform"/studio/{pyproject.toml,uv.lock} "$stage/platform"
cp "$platform/studio/agents/gateway.py" "$stage/platform"
cp "$game/studio/agent_review.py" "$stage/game/"
cp "$game"/core/{pier.h,pier.cpp,campaign.inc} "$stage/game/core/"
cp "$game/tools/balance.cpp" "$stage/game/tools/"
cp "$game/studio/Dockerfile" "$stage"
docker build -t studio-review:ci "$stage"
