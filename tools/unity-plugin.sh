#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
case "$(uname -s)" in
  Darwin) dir=engines/Unity/Assets/Plugins/macOS; flags=(-dynamiclib -arch arm64 -arch x86_64); file=libmosaic.dylib ;;
  Linux) dir=engines/Unity/Assets/Plugins/x86_64; flags=(-shared -fPIC); file=libmosaic.so ;;
  *) echo 'Build the native plug-in on macOS or Linux.' >&2; exit 1 ;;
esac
mkdir -p "$dir"
c++ -std=c++17 -O2 "${flags[@]}" core/pier.cpp -o "$dir/$file"
