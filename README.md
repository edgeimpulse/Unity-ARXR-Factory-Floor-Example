# Unity AR/XR Factory Floor Example — Meta Quest 3 / 3S

A virtual factory-floor XR experience built in **Unity 6 (6000.0.32f1)** for the
**Meta Quest 3 / 3S**, using an **Edge Impulse** vision model to inspect products
on a moving conveyor belt.

Bottles travel along a conveyor. A virtual inspection camera runs an Edge Impulse
**FOMO object-detection** model (bottle-cap detection). Bottles whose cap is
classified as **`cap_incorrect`** are flagged as defective and removed from the
line (the bottle "breaks"), while **`cap_correct`** bottles pass through.

> Status: **work in progress.** The Unity project, factory/conveyor assets and the
> bottle logic are in place. On-device Edge Impulse inference (macOS Editor for the
> simulator + Android arm64 for the headset) is being wired in.

## Edge Impulse model

- Public project (clone it yourself):
  <https://studio.edgeimpulse.com/public/751472/live>
- Task: object detection (FOMO)
- Labels: `cap_correct`, `cap_incorrect`

  <img width="768" height="732" alt="image" src="https://github.com/user-attachments/assets/cc13d0ce-6b17-45f8-92aa-0353cfc49d0f" />


The model is deployed as a **C++ library** and compiled to a native plugin for each
target platform. See Edge Impulse's
[Deploy your model as a C++ library](https://docs.edgeimpulse.com/docs/deploy-your-model-as-a-c-library).

> **Note:** never commit your Edge Impulse API key. Keep it in an environment
> variable or an untracked file (see `.gitignore`).

## Download & try it

A **nightly APK** is built automatically by GitHub Actions and published on the
[Releases page](https://github.com/edgeimpulse/Unity-ARXR-Factory-Floor-Example/releases/tag/nightly).
Download the latest `.apk` and sideload it:

```bash
adb install -r FactoryFloorXR-nightly-latest.apk
```

See [docs/CI-NIGHTLY-BUILD.md](docs/CI-NIGHTLY-BUILD.md) for the CI setup and the
repository secrets required to enable the build.

## Requirements

- Unity **6000.0.32f1**
- Meta Quest 3 or Quest 3S (Android build target)
- Packages (already in `Packages/manifest.json`):
  - Universal Render Pipeline (URP)
  - XR Interaction Toolkit 3.1.1
  - XR Hands 1.5.0
  - OpenXR 1.14.1

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/SampleScene.unity` | Main XR scene (build scene) |
| `Assets/Factory Conveyor/` | Conveyor models, materials and prefabs |
| `Assets/Bottle/` | Bottle + broken-bottle prefabs and scripts |
| `Assets/XR*`, `Assets/Samples/` | XR rig, hands and interaction samples |

## Testing in the simulator

The scene can be exercised in the Unity Editor with the **XR Device Simulator**
(from the XR Interaction Toolkit) — a mouse/keyboard-driven HMD and controllers —
so you can validate the conveyor and inspection flow without a headset.

## License

To be confirmed by Edge Impulse. This example builds on the Apache-2.0 licensed
[Edge Impulse Unity inferencing example](https://github.com/edgeimpulse/example-standalone-inferencing-unity).
