#!/usr/bin/env bash
# Download the Edge Impulse C++ library for this project and unpack it into
# native/ (edge-impulse-sdk/, model-parameters/, tflite-model/).
#
# Usage:
#   export EI_API_KEY=ei_xxxxxxxx        # from your Edge Impulse project
#   ./fetch_model.sh
#
# Optional overrides:
#   EI_PROJECT_ID (default 751472)   EI_IMPULSE_ID (default 1)
#
# The public project is at https://studio.edgeimpulse.com/public/751472/live —
# clone it into your own account to get an API key, or point these variables at
# your own project.
set -euo pipefail
cd "$(dirname "$0")"

: "${EI_API_KEY:?Set EI_API_KEY to your Edge Impulse API key}"
PROJECT_ID="${EI_PROJECT_ID:-751472}"
IMPULSE_ID="${EI_IMPULSE_ID:-1}"
BASE="https://studio.edgeimpulse.com/v1/api/${PROJECT_ID}"
H="x-api-key: ${EI_API_KEY}"

echo "Triggering C++ library build for project ${PROJECT_ID} (impulse ${IMPULSE_ID})..."
JOB="$(curl -s -X POST -H "${H}" -H "Content-Type: application/json" \
  -d '{"engine":"tflite"}' \
  "${BASE}/jobs/build-ondevice-model?type=zip&impulseId=${IMPULSE_ID}" \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')"
echo "Build job ${JOB} started; waiting..."

for _ in $(seq 1 60); do
  sleep 5
  DONE="$(curl -s -H "${H}" "${BASE}/jobs/${JOB}/status" \
    | python3 -c 'import sys,json;j=json.load(sys.stdin).get("job",{});print(j.get("finished") or "")')"
  [ -n "${DONE}" ] && break
  printf '.'
done
echo

echo "Downloading C++ library..."
curl -s -H "${H}" "${BASE}/deployment/download?type=zip&impulseId=${IMPULSE_ID}" -o /tmp/ei_cpp.zip
rm -rf edge-impulse-sdk model-parameters tflite-model
unzip -q -o /tmp/ei_cpp.zip -d .
echo "Done. SDK unpacked into native/."
