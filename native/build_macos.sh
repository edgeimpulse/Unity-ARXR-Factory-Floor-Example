#!/usr/bin/env bash
# Build libei_fomo.dylib for the Unity Editor / Meta XR Simulator on macOS
# (Apple Silicon) and copy it into Assets/Plugins/macOS/.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -d edge-impulse-sdk ]; then
  echo "Edge Impulse SDK not found. Run ./fetch_model.sh first (needs EI_API_KEY)." >&2
  exit 1
fi

JOBS="$(sysctl -n hw.ncpu 2>/dev/null || echo 4)"
make clean
make -j"${JOBS}" \
  CC=clang CXX=clang++ \
  SHARED="-dynamiclib" \
  NAME="libei_fomo.dylib"

DEST="../Assets/Plugins/macOS"
mkdir -p "${DEST}"
cp build/libei_fomo.dylib "${DEST}/libei_fomo.dylib"
echo "Installed ${DEST}/libei_fomo.dylib"
