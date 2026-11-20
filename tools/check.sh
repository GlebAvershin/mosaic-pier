#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p build
c++ -std=c++17 -Wall -Wextra -Werror -O1 -g -fsanitize=undefined,address core/pier.cpp tools/test-rules.cpp -o build/check
build/check > build/native.txt
node tools/test-wasm.mjs > build/wasm.txt
diff -u build/native.txt build/wasm.txt
c++ -std=c++17 -O2 core/pier.cpp tools/balance.cpp -o build/balance
cat build/native.txt
