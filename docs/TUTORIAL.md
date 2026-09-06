# Tutorial: Factory-Floor Cap Inspection in XR with Edge Impulse

This walkthrough takes you from zero to a working **Meta Quest 3 / 3S** experience
where a conveyor belt carries bottles past a virtual inspection station. An
**on-device Edge Impulse** vision model looks at each bottle's cap and decides
whether it is good (`cap_correct`) or defective (`cap_incorrect`) — defective
bottles are rejected and shatter.

It's designed as a **learning example**: every part is built in plain C# at
runtime, so you can read one file and understand the whole pipeline.

![The conveyor demo](media/factory-demo.gif)

---

## 1. What you'll learn

- How to run an Edge Impulse **FOMO object-detection** model **on-device** from
  Unity through a small native plugin (no cloud calls).
- How to feed pixels from Unity into the model and act on the result.
- How the XR scene, conveyor logic and inspection station fit together.
- How to build and sideload the APK, and how the nightly CI build works.

## 2. Fastest way to try it

Download the latest APK from the
[**Releases**](https://github.com/edgeimpulse/Unity-ARXR-Factory-Floor-Example/releases)
page and sideload it onto a Quest 3 / 3S (developer mode) or an arm64 Android
device:

```bash
adb install -r FactoryFloorXR-v0.1.0.apk
```

## 3. The Edge Impulse model

- Public project (clone it into your own account): <https://studio.edgeimpulse.com/public/751472/live>
- Type: **object detection (FOMO)**, 96×96 RGB input
- Labels: `cap_correct`, `cap_incorrect`

The model was trained on photographs of bottle caps. In the demo, each virtual
bottle is assigned a real cap photo which the inspection station shows the model —
so the inference you see is genuinely running the trained network.

## 4. Prerequisites

| To… | You need |
| --- | --- |
| Run the prebuilt APK | A Quest 3/3S in developer mode, or an arm64 Android phone |
| Open & edit the project | Unity **6000.0.32f1** |
| Rebuild the native model libraries | Xcode command-line tools (macOS) and/or the Android NDK **r23b** |
| Build an APK locally | Unity Android module, Android SDK + NDK r23b + a JDK |

## 5. Open the project

1. Clone the repo and open it in Unity 6000.0.32f1.
2. Open `Assets/Scenes/FactoryFloorDemo.unity`.
3. Press **Play**. Bottles start flowing; the monitor shows each cap photo and the
   verdict; the HUD counts Passed / Rejected. If the native model is present the
   HUD reads *"Edge Impulse: on-device inference"*.

> The scene contains a single `FactoryDemo Bootstrap` object. Everything else —
> belt, spawner, inspection station, monitor and HUD — is created in code by
> [`FactoryDemoBootstrap`](../Assets/FactoryDemo/Scripts/FactoryDemoBootstrap.cs)
> when you press Play, so it's easy to read and tweak.

## 6. How it works

```
BottleSpawner ──spawns──> Bottle (+ ProductItem, a real cap photo)
      │                         │
      │   moves along belt      ▼
ConveyorBelt ───────────> InspectionStation.Inspect(item)
                                  │
                                  ▼
                    EdgeImpulseFOMO.TryClassifyTexture(photo)
                                  │   (native libei_fomo)
                    cap_incorrect ▼ cap_correct
                    Bottle.Explode()   pass → despawn
```

| Script | Responsibility |
| --- | --- |
| [`FactoryDemoBootstrap`](../Assets/FactoryDemo/Scripts/FactoryDemoBootstrap.cs) | Builds and wires the whole scene at runtime |
| [`ConveyorBelt`](../Assets/FactoryDemo/Scripts/ConveyorBelt.cs) | Belt path, speed and the scrolling belt texture |
| [`BottleSpawner`](../Assets/FactoryDemo/Scripts/BottleSpawner.cs) | Spawns bottles, advances them, retires them |
| [`InspectionStation`](../Assets/FactoryDemo/Scripts/InspectionStation.cs) | Runs the model and decides pass/reject |
| [`EdgeImpulseFOMO`](../Assets/FactoryDemo/Scripts/EdgeImpulseFOMO.cs) | P/Invoke wrapper around the native library |
| [`FactoryHUD`](../Assets/FactoryDemo/Scripts/FactoryHUD.cs) | Pass/reject counters + inference-mode label |

The inspection logic is deliberately simple: **no detection = pass**, a
`cap_incorrect` detection above the threshold = reject. If the native library
isn't available on a platform, the station falls back to a simulated result so
the demo always runs.

## 7. The native model library

Edge Impulse deploys the model as a **C++ library**. A thin wrapper,
[`native/ei_fomo.cpp`](../native/ei_fomo.cpp), exposes three C functions
(`ei_fomo_classify`, `ei_fomo_input_width`, `ei_fomo_input_height`) that the C#
side calls. Build it per platform:

```bash
cd native
export EI_API_KEY=ei_xxx      # from your Edge Impulse project
./fetch_model.sh              # downloads the C++ library for project 751472
./build_macos.sh              # -> Assets/Plugins/macOS/libei_fomo.dylib   (Editor)
./build_android.sh            # -> Assets/Plugins/Android/arm64-v8a/libei_fomo.so (Quest)
```

Prebuilt libraries are already committed, so you only need this if you retrain the
model or target a new platform. After (re)building, run **Tools → nothing** — the
plugins are auto-configured by
[`EIPluginSetup`](../Assets/Editor/EIPluginSetup.cs).

## 8. Build the APK yourself

- **In the Editor:** switch the platform to Android and use
  [`FactoryBuild.BuildAndroid`](../Assets/Editor/FactoryBuild.cs), or from the
  command line:
  ```bash
  Unity -batchmode -quit -projectPath . -buildTarget Android \
        -executeMethod FactoryBuild.BuildAndroid
  ```
- **Nightly CI:** the [`Nightly APK Build`](../.github/workflows/nightly-apk.yml)
  workflow builds and publishes a rolling `nightly` release. See
  [CI-NIGHTLY-BUILD.md](CI-NIGHTLY-BUILD.md) for the required secrets.

> Unity 6000.0.32f1 requires **NDK r23b (23.1.7779620)**. The build script points
> Unity at your installed SDK/NDK automatically.

## 9. Use your own model

1. Clone the public project (or build your own bottle-cap detector) in Edge Impulse.
2. Set `EI_API_KEY` and, if needed, `EI_PROJECT_ID` / `EI_IMPULSE_ID`, then rerun
   the `native/` scripts.
3. Keep the label names (`cap_correct` / `cap_incorrect`) or update
   `InspectionStation.defectLabel` to match your model.

## 10. Troubleshooting

| Symptom | Fix |
| --- | --- |
| HUD says *"simulated (native lib not loaded)"* | The native plugin isn't present/enabled for this platform — rebuild it (section 7). |
| APK build: *"Android SDK/NDK not found"* | Install the SDK's Command-line Tools and **NDK r23b**; the build script sets the paths. |
| Model detects nothing on a photo | FOMO is spatial — the cap must be reasonably centred/framed like the training images. |
| Package resolve fails behind a proxy | Trust your corporate CA via `NODE_EXTRA_CA_CERTS` before launching Unity. |
