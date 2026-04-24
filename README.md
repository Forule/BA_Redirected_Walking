# 🥽 Redirected Walking VR Environment

## Table of contents
* [Introduction](#introduction)
* [Deployment](#deployment)
* [Architecture](#architecture)
* [Third-Party Assets](#third-party-assets)
* [License](#license)
* [Forbidden](#forbidden)

---

## Introduction
This repository contains a specialized Virtual Reality (VR) test environment developed in **Unity** to evaluate advanced **Redirected Walking (RDW)** techniques. The project is built using the **Vive Wave SDK** and is specifically optimized for the **HTC Vive Focus Vision**.

A key feature of this implementation is **Blink-Induced Redirection**. By leveraging integrated **Eye Tracking technology**, the system detects user blinks in real-time and applies spatial redirection gains during these periods of visual suppression, maximizing the undetectability of movement manipulation.

## Deployment
This project is developed specifically for the **Vive Wave platform**:
* **Engine Requirement:** Built with Unity.
* **Hardware Target:** **HTC Vive Focus Vision** (required for full eye-tracking feature support).
* **SDK:** Developed using the **Vive Wave SDK**.
* **Build Process:** The project is compiled as an Android APK tailored for the Wave VR runtime. It can be deployed directly via the Unity Build Pipeline or side-loaded onto the headset.

## Architecture
The architecture is designed to handle high-frequency biometric data within the Vive Wave ecosystem:

1.  **Vive Wave SDK Integration (C#):** The entire XR logic, including 6DoF tracking and input handling, is based on the Vive Wave SDK. This ensures deep integration with the headset's hardware-level tracking capabilities.
2.  **Custom Blink Detection Logic:** The system pulls raw **"Eye Openness"** data streams directly via the headset's eye-tracking capabilities. A custom C# algorithm analyzes these values in real-time, detecting blink events through defined thresholds without relying on simplified event triggers.
3.  **Blink-Triggered Redirection:** Upon blink detection, the system applies mathematical redirection gains (translation, rotation, and curvature) to the virtual camera. This minimizes "simulator sickness" by masking the world-shift during the user's natural moments of blindness.
4.  **Performance Optimization:** The logic is optimized for the standalone mobile processor of the Vive Focus Vision to ensure a stable framerate, maintaining immersion and tracking precision.

## Third-Party Assets
To focus development resources entirely on the complex C# logic and RDW algorithms, the visual presentation relies on external resources. All 3D models, textures, environment graphics, and underlying shaders (ShaderLab/HLSL) utilized in this test environment are **free assets acquired from the Unity Asset Store** and were not created by the author.

## License
Copyright (c) 2026 Jan Muljowin  
This project is licensed under the **MIT License**.

## Forbidden
**Hold Liable:** The software is provided "as is", without warranty of any kind. The software author or license owner cannot be held liable for any damages or issues arising from the use of this software.
