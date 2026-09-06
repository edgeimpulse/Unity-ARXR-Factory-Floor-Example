#!/usr/bin/env bash
# Build libei_fomo.so for Meta Quest 3 / 3S (Android arm64-v8a) and copy it into
# Assets/Plugins/Android/arm64-v8a/.
#
# Uses $ANDROID_NDK_ROOT if set, otherwise the NDK bundled with the Unity 6
# Android module.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -d edge-impulse-sdk ]; then
  echo "Edge Impulse SDK not found. Run ./fetch_model.sh first (needs EI_API_KEY)." >&2
  exit 1
fi

# --- locate the NDK -------------------------------------------------------
NDK="${ANDROID_NDK_ROOT:-}"
if [ -z "${NDK}" ]; then
  for base in \
    "/Applications/Unity/Hub/Editor"/*/Editor/Data/PlaybackEngines/AndroidPlayer/NDK \
    "/Applications/Unity/Hub/Editor"/*/PlaybackEngines/AndroidPlayer/NDK \
    "${HOME}/Library/Android/sdk/ndk"/* ; do
    if [ -d "${base}" ]; then NDK="${base}"; break; fi
  done
fi
if [ -z "${NDK}" ] || [ ! -d "${NDK}" ]; then
  echo "Android NDK not found. Set ANDROID_NDK_ROOT to your NDK path." >&2
  exit 1
fi
echo "Using NDK: ${NDK}"

# --- host toolchain dir ---------------------------------------------------
HOST="darwin-x86_64"
[ -d "${NDK}/toolchains/llvm/prebuilt/${HOST}" ] || HOST="$(ls "${NDK}/toolchains/llvm/prebuilt" | head -n1)"
TOOLS="${NDK}/toolchains/llvm/prebuilt/${HOST}/bin"
API=24

JOBS="$(sysctl -n hw.ncpu 2>/dev/null || echo 4)"
make clean
make -j"${JOBS}" \
  CC="${TOOLS}/aarch64-linux-android${API}-clang" \
  CXX="${TOOLS}/aarch64-linux-android${API}-clang++" \
  SHARED="-shared" \
  NAME="libei_fomo.so"

DEST="../Assets/Plugins/Android/arm64-v8a"
mkdir -p "${DEST}"
cp build/libei_fomo.so "${DEST}/libei_fomo.so"
echo "Installed ${DEST}/libei_fomo.so"
