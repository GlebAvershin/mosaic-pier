#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
compiler="${WASM_CXX:-clang++}"
"$compiler" --target=wasm32 -std=c++17 -O2 -nostdlib -fno-exceptions -fno-rtti -fno-builtin \
  -Wl,--no-entry -Wl,--export-all -Wl,--strip-all core/pier.cpp tools/wasm_memory.cpp -o web/pier.wasm
