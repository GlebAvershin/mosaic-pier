#!/usr/bin/env bash
# Execute in a disposable Linux CI runner with Docker, kubectl, uv and kind.
set -euo pipefail
platform=$(cd "$1" && pwd)
game=$(cd "$2" && pwd)
export STUDIO_PROJECT=$3
out=$4
mkdir -p "$out"
out=$(cd "$out" && pwd)
work=$(mktemp -d)
export STUDIO_TOKENS='{"runner-design":"design","runner-qa":"qa"}'
cluster=review
pids=()
forwards=()
cleanup() {
  for pid in "${forwards[@]}" "${pids[@]}"; do kill "$pid" 2>/dev/null || true; done
  kind delete cluster --name "$cluster" >/dev/null 2>&1 || true
  docker rm -f review-postgres >/dev/null 2>&1 || true
  rm -rf "$work"
}
trap cleanup EXIT
cd "$work"
kind create cluster --name "$cluster" --wait 150s
bridge=$(docker exec "$cluster-control-plane" ip -4 route show default | awk 'NR == 1 {print $3}')
[[ "$bridge" =~ ^[0-9]+(\.[0-9]+){3}$ ]] || { echo "Missing cluster IPv4 gateway"; exit 1; }
docker run -d --rm --name review-postgres -e POSTGRES_PASSWORD=ci-local -e POSTGRES_DB=studio \
  -p "$bridge:25432:5432" postgres:16-bookworm
"$game/studio/image.sh" "$platform" "$game"
kind load docker-image studio-review:ci --name "$cluster"
curl --retry 3 -fsSL https://github.com/ggml-org/llama.cpp/releases/download/b12578/llama-b12578-bin-ubuntu-x64.tar.gz -o llama.tgz
curl --retry 3 -fsSL https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf -o qwen.gguf
curl --retry 3 -fsSL https://huggingface.co/HuggingFaceTB/SmolLM2-360M-Instruct-GGUF/resolve/main/smollm2-360m-instruct-q8_0.gguf -o smol.gguf
sha256sum -c <<'SUMS'
95db6800c6321c2c64360850e8fa5ea848c65ef3850205d2aea57eb0de001ca1  llama.tgz
74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db  qwen.gguf
48ab3034d0dd401fbc721eb1df3217902fee7dab9078992d66431f09b7750201  smol.gguf
SUMS
tar -xzf llama.tgz
binary=$(find "$work" -name llama-server -type f | head -1)
export LD_LIBRARY_PATH=$(dirname "$binary")
for route in qwen:18181 smol:18182; do
  model=${route%:*}
  port=${route#*:}
  "$binary" -m "$model.gguf" --alias "$model" --host "$bridge" --port "$port" -c 2048 -np 1 -t 2 >"$model.log" 2>&1 &
  pids+=($!)
  curl -fsS --retry 30 --retry-all-errors --retry-delay 1 "http://$bridge:$port/health"
done
server=/studio/game/agent_review.py
python=/studio/platform/.venv/bin/python
kubectl create secret generic review-config \
  --from-literal=STUDIO_PROJECT="$STUDIO_PROJECT" --from-literal=STUDIO_TOKENS="$STUDIO_TOKENS" \
  --from-literal=QWEN_URL="http://$bridge:18181" --from-literal=SMOL_URL="http://$bridge:18182" \
  --from-literal=STUDIO_DB="postgresql://postgres:ci-local@$bridge:25432/studio" \
  --from-literal=RELEASE_REVISION="$(git -C "$game" rev-parse HEAD)" \
  --from-literal=PLATFORM_REVISION="$(git -C "$platform" rev-parse HEAD)" \
  --from-literal=TOOL_PYTHON="$python" --from-literal=REVIEW_SERVER="$server"
kubectl apply -f "$platform/studio/agents/deployment.yaml"
forward() {
  kubectl rollout status deployment/game-review --timeout=180s || { kubectl get pods; kubectl logs -l app=game-review --tail=40; return 1; }
  # Terminating pods can still be listed immediately after a rolling restart.
  mapfile -t pods < <(kubectl get pods -l app=game-review -o json | jq -r '.items[] | select(.metadata.deletionTimestamp==null) | .metadata.name')
  test "${#pods[@]}" -eq 2
  for i in 0 1; do
    port=$((18283+i))
    kubectl port-forward "pod/${pods[$i]}" "$port:8080" >"forward-$i.log" 2>&1 &
    forwards+=($!)
    curl -fsS --retry 12 --retry-all-errors --retry-delay 1 "http://127.0.0.1:$port/ready"
  done
}
forward
uv run --project "$platform/studio" --locked python "$platform/studio/agents/probe.py" "$out/requests.json"
for pid in "${forwards[@]}"; do kill "$pid"; wait "$pid" || true; done
forwards=()
kubectl rollout restart deployment/game-review
forward
uv run --project "$platform/studio" --locked python "$platform/studio/agents/probe.py" "$out/restored.json" --restored
python3 - "$out" <<'PY'
import json, pathlib, sys
p=pathlib.Path(sys.argv[1])
before=json.loads((p/'requests.json').read_text())
after=json.loads((p/'restored.json').read_text())
workers=lambda phase: {r['worker'] for r in phase['responses']}
assert workers(before['fresh']).isdisjoint(workers(after['restored']))
PY
