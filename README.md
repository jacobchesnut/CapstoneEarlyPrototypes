# Capstone Early Prototypes

## Overview

This repository contains Unity prototypes exploring eye tracking and foveated rendering for XR. It includes experimental rendering pipelines, compute shaders, calibration and eye-tracking demos, and scenes for testing rendering quality and performance. It is a collection of research and development prototypes rather than a single finished application; individual scenes may require different setup.

This project is based off of the ray trace renderer by Joe Leung, [A User Programmable System Agnostic Real-Time Ray Tracing Framework](https://sites.uw.edu/crcs/projects/2021a-user-programmable-system-agnostic-real-time-ray-tracing-framework/).

The Unity project is in [`Prototypes`](Prototypes/).

## Prerequisites

- **Unity 2022.3.17f1**. The project version is recorded in `Prototypes/ProjectSettings/ProjectVersion.txt`.
- A graphics device and driver that support Unity compute shaders for the custom rendering experiments.
- Unity packages are declared in `Prototypes/Packages/manifest.json`. Open the project with the specified Unity version and allow Unity Package Manager to resolve them.
- For XR experiments, a compatible headset and runtime. The project includes Meta XR and Oculus/OpenXR packages and loader settings; configure the loader and build target for the device you intend to use.
- Some eye-tracking and calibration experiments expect calibration data files. See [Calibration data](#calibration-data).

## Open and run

1. Install Unity 2022.3.17f1 through Unity Hub.
2. In Unity Hub, choose **Add** or **Open**, and select the `Prototypes` directory.
3. Allow Unity to import the assets and resolve packages.
4. Open `Assets/Scenes/EyeTrackingVerification.unity` and press **Play** to try the scene currently enabled in Build Settings.
5. To run on an XR device, connect and configure the headset/runtime, select the appropriate XR loader and platform in **Project Settings**, then use **Build and Run** for your target platform.

Other scenes in `Assets/Scenes/` include calibration, dynamic, quality, stress-test, and rendering experiments. Not every scene is configured as a standalone entry point, so inspect its objects and component references in the Unity Editor before running it.

## Code highlights

| Area | Where to look | What it contains |
|---|---|---|
| Custom render pipeline | `Prototypes/Assets/SRP/RenderPipeline/BasicPipeInstance.cs` | Render pipeline implementation, scene/render setup, and compute-shader dispatch. |
| Pipeline experiment driver | `Prototypes/Assets/Scripts/ComputeShaderTesting/TryCreateJoePipeline.cs` | Sets up the custom pipeline, render textures, foveation options, and calibration input. |
| Compute shaders | `Prototypes/Assets/SRP/ComputeShader/` | Ray generation, intersection, lighting, and shading shader code. |
| Scene geometry and acceleration structures | `Prototypes/Assets/SRP/Scene/` | Ray-tracing scene parsing, geometry, and bounding-volume hierarchy (BVH) structures. |
| Shader foveation experiment | `Prototypes/Assets/Scripts/ShaderFoveated/SceneControl.cs` | Foveation controls, test settings, and shader/material parameter updates. |
| Camera render integration | `Prototypes/Assets/Scripts/ShaderFoveated/CameraRenderCall.cs` | Captures camera render callbacks and passes gaze/frustum information into the scene-control logic. |
| Foveation visualization | `Prototypes/Assets/Scripts/3DFoveatedRepresentation/ScreenGenerator.cs` | Builds a simple screen grid and demonstrates distance- and angle-based regions. |
| Eye-tracking verification | `Prototypes/Assets/Scripts/Eye Tracking Demo/TheWorld.cs` | Visualizes eye gaze, raycast hit points, and gaze-related measurements. |

## Calibration data

Some scripts use hard-coded Windows paths under `C:\Users\jakee\CapstoneShared\`, including `Calibration.dat` and `FoveatedCalibration.dat`. The calibration reader/writer scripts expect binary float values in a specific order, so simply creating empty files will not work. These paths are machine-specific and are not suitable defaults for other contributors.

If a scene fails on startup with a missing calibration file, either provide the expected calibration data at the path used by that script or update the relevant script to use a local, configurable path and the matching file format. Calibration-dependent experiments may not work out of the box.

## Project layout

```text
Prototypes/
  Assets/
    Scenes/       Unity scenes and experiments
    Scripts/      Prototype behaviors and experiment drivers
    SRP/          Custom rendering pipeline, shaders, and ray-tracing code
  Packages/       Unity package manifest and lock file
  ProjectSettings/
```

## Project status

This is an early-prototype codebase. Expect experimental behavior, scene-specific setup, and unfinished areas; use the Unity Editor and the relevant scene scripts to understand an experiment's configuration.
