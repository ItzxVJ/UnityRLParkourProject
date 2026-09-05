# Project Overview
- **Game Title:** AI-Parkour (ML-Agents Cube Parkour Training)
- **High-Level Concept:** A visually stunning, synthwave/neon-themed reinforcement learning parkour environment where a Rigidbody-controlled cube learns to navigate complex obstacle courses through curriculum training.
- **Players:** Single Agent (Reinforcement Learning Agent) with full keyboard Heuristic support for manual playtesting.
- **Inspiration / Reference Games:** *Tron*, *Beat Saber*, *Only Up*, *Ghostrunner*.
- **Tone / Art Direction:** Sleek, high-contrast **Synthwave/Neon Aesthetic** tailored for high-quality video content (YouTube). Features glowing emissive materials, deep dark charcoal environments, and vibrant neon accents (Cyan, Magenta, Lime Green, and Golden Orange) backed by rich URP Post-Processing.
- **Target Platform:** PC (Windows) / YouTube demonstration.
- **Screen Orientation / Resolution:** Landscape 1920x1080 (HD/4K).
- **Render Pipeline:** Universal Render Pipeline (URP) with active HDR and real-time shadows.

# Game Mechanics
## Core Gameplay Loop
The glowing agent cube resets at the **Neon Cyan** start platform of the active level. It must utilize horizontal forces and jump impulses to cross gaps, navigate narrow beams, time moving platforms, and reach the glowing **Neon Green** goal platform.
- **Success:** Reaching the goal platform (+2.0 reward, episode ends).
- **Failure:** Falling below Y < -5.0 (-1.0 reward, episode ends).
- **Efficiency:** A constant step penalty (-0.001 per step) encourages rapid, optimized pathfinding.

## Controls and Input Methods
- **Agent Actions (Continuous Actions, Size 3):**
  - **Action 0:** Forward/Backward force application (Range: -1.0 to +1.0)
  - **Action 1:** Left/Right force application (Range: -1.0 to +1.0)
  - **Action 2:** Jump trigger (Range: -1.0 to +1.0. If action > 0.5 and the agent is grounded, apply an upward jump impulse)
- **Manual Heuristic Mode (Keyboard Controls):**
  - **W / S / Up / Down:** Move Forward / Backward
  - **A / D / Left / Right:** Move Left / Right
  - **Spacebar:** Trigger Jump

# Visual Aesthetics & Room Design (Sleek Synthwave Arena)
To ensure the levels look exceptionally polished and visually stunning for YouTube, the environments will be automatically built with the following custom assets, lighting, and layout features:
1. **Camera & Backdrop:**
   - Deep solid charcoal-dark background (`#0E0F14` or `#08080C`) instead of a generic skybox. This makes neon colors pop.
   - Main Camera configured with `renderPostProcessing = true`.
2. **URP Lit Shader Styling:**
   - Highly polished, semi-glossy, metallic materials (Smoothness = 0.6, Metallic = 0.2) on all structures.
   - Glowing HDR Emissive materials utilizing URP's `_EmissionColor` and enabling the `_EMISSION` keyword:
     - **Agent Cube:** Glowing White/Cyan (`#D0F8FF` / Emission Intensity = 1.5)
     - **Start Platform:** Vibrant Neon Cyan (`#00F0FF` / Emission Intensity = 2.5)
     - **Goal Platform:** Vibrant Neon Lime Green (`#00FF66` / Emission Intensity = 3.0)
     - **Static Obstacles/Platforms:** Glossy Deep Slate Gray (`#1E1F29` with subtle cyan accent rims)
     - **Moving Platforms:** Glowing Neon Golden Orange (`#FF9900` / Emission Intensity = 2.5)
     - **Dangerous/Alternative Walls:** Vibrant Neon Magenta (`#FF0055` / Emission Intensity = 2.0)
3. **Pillar & Boundary Framing ("The Grid"):**
   - Each level is set within a framed "Arena" outlined by two parallel rows of glowing neon columns/pillars along the left and right borders. This bounds the scene beautifully and provides a sense of speed and progression as the cube zooms past.
4. **Cinematic URP Post-Processing Volume:**
   - **Bloom:** Threshold = 0.85, Intensity = 2.0, Scatter = 0.7. Gives a gorgeous glowing dreamscape look.
   - **ACES Tonemapping:** Rich cinematic colors, deeper blacks, and vibrant contrast.
   - **Vignette:** Intensity = 0.35, Smoothness = 0.4. Adds a polished framing border.
   - **Color Adjustments:** Contrast = 15, Saturation = 15. Makes colors pop.

# Key Asset & Context
### 1. `ParkourAgent.cs`
- Class inheriting from `Unity.MLAgents.Agent`.
- Handles continuous actions, velocity tracking, distance check, ground checking via custom downward raycasts, and full WASD Heuristics.
- Restricts Rigidbody rotation (X/Z axes frozen) for clean platformer physics.

### 2. `ParkourEnvironment.cs`
- Standardizes environment setup. Handles agent resetting, level selection, falling checks, and goal trigger events.
- Disables/Enables level models based on `levelIndex` loaded dynamically from the ML-Agents training configuration or inspector settings.

### 3. `MovingPlatform.cs`
- Component that moves platforms smoothly along specified local directions using a sine wave function.
- Resets seamlessly on episode start to guarantee reproducible training sequences.

### 4. `ParkourLevelBuilder.cs` (Editor Tool)
- Adds `Parkour/Build All Synthwave Levels` to the Unity menu.
- Generates all materials with matching URP HDR Lit properties.
- Procedurally constructs 5 distinct level scenes, complete with lighting, camera setup, post-processing volume, and decorative neon pillars.
- Generates a `CurriculumTraining` scene featuring a grid of 9 parallel arenas for supercharged multi-agent training.

### 5. `parkour_curriculum.yaml`
- Complete YAML config specifying the PPO network parameters, hyper-parameters, and a 5-step curriculum using the `level_index` parameter.

# Implementation Steps
All steps are executed sequentially by the developer.

### Step 1: Implement Core Logic Scripts
- **Description:** Implement `ParkourAgent.cs`, `ParkourEnvironment.cs`, and `MovingPlatform.cs` under `Assets/Scripts/`.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Implement Level Builder Script
- **Description:** Implement `ParkourLevelBuilder.cs` under `Assets/Scripts/Editor/` containing shader property mapping, post-processing configuration, and the procedural generation layout of the 5 rooms (with neon framing, pillars, and materials).
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Run Level Builder
- **Description:** Trigger the generator via the Editor menu to build the 5 scenes and the `CurriculumTraining` scene, ensuring all materials, lighting, and volumes are automatically set up.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 4: Write Training Config YAML
- **Description:** Create the curriculum YAML training file (`parkour_curriculum.yaml`) under `Assets/Config/`.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Yes

# Verification & Testing
1. **Compilation Check:** Verify that all generated C# scripts compile without any warnings or errors.
2. **Heuristics Verification:** Open each of the generated levels, enter Play Mode, and manually play using keyboard WASD + Space. Confirm that:
   - The cube moves smoothly and jumps correctly.
   - Touching the green goal platform triggers a successful end of the episode and logs a positive reward.
   - Falling below the Y threshold resets the agent immediately and logs a negative reward.
   - Moving platforms animate smoothly and reset positions correctly.
3. **Training Verification:** Verify the ML-Agents setup compiles and the CLI command format is ready for local training.
