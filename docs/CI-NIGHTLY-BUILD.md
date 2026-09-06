# Nightly APK build (GitHub Actions)

The [`Nightly APK Build`](../.github/workflows/nightly-apk.yml) workflow builds a
sideloadable Android/Quest APK every night at 03:00 UTC (and on demand from the
**Actions** tab) using [GameCI](https://game.ci). Each successful build:

- uploads the APK as a workflow **artifact** (`FactoryFloorXR-APK`), and
- updates a rolling **`nightly` pre-release** so anyone can download the latest
  `.apk` from the repo's **Releases** page.

## Install the APK

```bash
# Quest 3 / 3S (developer mode + USB) or any Android device
adb install -r FactoryFloorXR-nightly-latest.apk
```

## Required repository secrets

Add these under **Settings → Secrets and variables → Actions → New repository
secret**. Until all three exist, the build job is **skipped** (not failed).

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Full contents of your `Unity_v6000.x.ulf` activation file |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

This project uses a **Unity Personal** license, so you must supply the activation
file (`.ulf`). Generate it once:

1. Temporarily add a job that runs
   [`game-ci/unity-request-activation-file@v2`](https://github.com/game-ci/unity-request-activation-file),
   or run the Editor locally with
   `Unity -batchmode -createManualActivationFile -logfile` to produce a `.alf`.
2. Upload the `.alf` at <https://license.unity3d.com/manual> and download the
   resulting `Unity_v6000.x.ulf`.
3. Paste the **entire** `.ulf` file contents into the `UNITY_LICENSE` secret.

> The GameCI Android image already includes the Android build support, IL2CPP and
> NDK, so no extra modules are required in CI.

## Optional: signed release builds

By default the APK is signed with a debug keystore (fine for sideloading). To
sign with your own keystore, add `ANDROID_KEYSTORE_BASE64`,
`ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` and `ANDROID_KEYALIAS_PASS`
secrets and pass them to the `game-ci/unity-builder` step.
