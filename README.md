# 🥽 Redirected Walking VR — Bachelor Thesis (1.0)

[![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![HTC Vive](https://img.shields.io/badge/HTC_Vive_Focus_Vision-Standalone_VR-blue?style=for-the-badge)](https://www.vive.com/)
[![Grade](https://img.shields.io/badge/Grade-1.0_%2F_Top_Mark-gold?style=for-the-badge)]()

> **"Grenzen des Redirected Walkings — Ermittlung des minimal benötigten Raums für Redirected Walking in VR-Anwendungen"**  
> Bachelor Thesis · Hochschule Offenburg · Media & Computer Science · September 2025

---

## Overview

This repository contains the VR research environment developed for my Bachelor's thesis, which investigated the psychophysical limits of two core **Redirected Walking (RDW)** techniques — and what they mean for real-world applicability in home settings.

Redirected Walking allows users to walk infinitely in a virtual space while physically moving within a small room, by subtly manipulating the virtual environment. The key challenge: the manipulation must remain below the user's **perception threshold** — otherwise immersion breaks and cybersickness sets in.

---

## Research Questions

1. What are the **75% detection thresholds (JND)** for Blink-Induced Rotation and Curvature Gains during continuous forward walking?
2. How does **subjective discomfort and cybersickness** develop for each method as manipulation intensity increases?
3. What is the **minimum physical space** required for imperceptible, infinite walking using each method at its JND?

---

## Key Findings

| Method | 75% Threshold (JND) | Min. Space Required |
|---|---|---|
| Curvature Gains | **4.29°/m** | **~27 × 27 m (714 m²)** |
| Blink-Induced Rotation | **3.05°/Blink** | *(event-based, no direct radius)* |

- Curvature Gains produced a **significantly steeper discomfort increase** with rising intensity than Blink-Induced Rotation (LMM interaction: β = 0.096, p < .001)
- SSQ analysis confirmed a **significant increase in cybersickness** across all subscales post-exposure (Total Score: t(24) = 4.12, p < .001)
- Central conclusion: **purely imperceptible RDW requires unrealistically large spaces** for home use — supporting the paradigm shift from *imperceptibility* to *applicability*

---

## Methodology

**Participants:** N = 30 recruited (N = 25 final after data quality screening)

**Hardware:** HTC Vive Focus Vision (standalone, no PC required) — inside-out tracking up to 10×10m, integrated eye tracking, 90Hz LCD panels

**Experimental Design:** Within-participant, counterbalanced block order (Curvature Gains ↔ Blink-Induced Rotation)

**Psychophysical Method:** Adaptive Staircase (coarse + fine phase) with PEST-based step size reduction; trials used a **2AFC paradigm** with 15% catch trials for validity

**Statistical Analysis:**
- Bayesian **GLMM** (generalized linear mixed model with logit link) for psychometric function fitting — random intercepts and slopes per participant
- **LMM** (linear mixed model) for subjective discomfort analysis
- Paired t-tests for pre/post SSQ comparison
- Posterior simulation (N = 2000) for 95% credibility intervals

**Measures:**
- **SSQ** (Simulator Sickness Questionnaire) — pre and post exposure
- **Likert discomfort scale** (1–5) after each fine-phase trial
- **Confidence rating** (1–5) alongside each 2AFC response

---

## Technical Implementation

Built in **Unity 2022.3.5f1 LTS** using the **Vive Wave SDK** and Unity XR Plugin Management. All C# logic written in Visual Studio 2022.

**Custom Scripts:**
- `RDWExperimentManager.cs` — controls the full experimental flow: staircase logic, block management, data logging (CSV), trial randomization, catch trial injection
- `RedirectedWalkingManager.cs` — applies Curvature Gain rotations and Blink-Induced Rotations to the virtual world
- `FloorSnapper.cs` — realigns the VR rig to the virtual floor after each scene reset

**Blink Detection Workaround:** Due to eye-tracking hardware latency on the Vive Focus Vision, a **200ms blackout phase** was introduced upon blink detection to ensure the rotation was applied before the user's eyes reopened — effectively masking the world-shift without relying on precise millisecond timing.

**Virtual Environment:** Low-poly desert scene with a defined coin-collection path. A 180° scene reset at the end of each pass allowed the participant to walk back physically in the opposite direction — maximizing use of the 10×10m tracking area without resets.

**Data pipeline:** Automated Python preprocessing script for CSV harmonization, outlier removal, and conversion to binary long-format for GLMM input.

---

## Third-Party Assets

All 3D models, textures, and shaders are free assets from the Unity Asset Store and were not created by the author. Development focus was entirely on the C# experiment logic and RDW algorithms.

---

## License

Copyright (c) 2026 Jan Muljowin — MIT License.
