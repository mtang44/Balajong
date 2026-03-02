# GPU Noise Background Shader (Unity)

This project includes a “GPU noise background” tool: a material/shader that **draws a moving, cloudy noise pattern in real time**. You can use it as a sky/background, a mood layer behind gameplay, or a stylized “nebula/static/lava” effect.

The main files are:

- `Assets/Shaders/NoiseBackgroundGPU.shader` (unlit background shader)
- `Assets/Shaders/NoiseBackgroundGPU_Lit.shader` (lit version that reacts to lights)
- `Assets/Scripts/Noise/NoiseBackgroundGPUController.cs` (optional script that animates the shader + fits a quad to the camera)
- Example materials: `Assets/Shaders/Example Materials/M_NoiseBG.mat`, `Assets/Shaders/Example Materials/M_NoiseBG 1.mat`

---

## What the tool generates

- **A procedural (generated) noise image**: “clouds”, “smoke”, “ink”, “static”, or “lava blobs”.
- **Animated motion**: the pattern can “evolve” over time and/or scroll in a direction.
- **Stylized colors**: it can use **4 color bands** (a small palette) for a posterized / retro look, or grayscale.

You do **not** have to import a texture image for this.

---

## How to run / access it

1. **Create a material**
   - In the Project window: Right-click → **Create → Material**
   - Name it something like `M_NoiseBG_MyVersion`
2. **Pick the shader**
   - In the material Inspector, set **Shader** to:
     - **Unlit**: `Noise/BackgroundGPU` (most “background” use-cases)
     - **Lit**: `Noise/BackgroundGPU Lit` (if you want lights/emission)
3. **Put it on a quad**
   - GameObject → **3D Object → Quad**
   - Drag the material onto the Quad (or set it in the Quad’s Mesh Renderer)
4. **Have fun**
    - Press Play and tweak the material values.

## Parameter explanations (what each one does)

These are the material parameters on `Noise/BackgroundGPU` (the lit shader adds a few more at the top).

### Coordinates (where the pattern “lives”)

- **Resolution (`_Resolution`)**: A “virtual canvas size” for how the pattern is mapped.
  - Bigger values make the coordinate space larger (often feels like the pattern is less “zoomed in”).
  - Typical: `(256, 144)` (matches the included defaults).
- **Aspect (`_Aspect`)**: Width/height of the camera.
  - If this is wrong, swirl/twist can look stretched.
  - The controller updates this automatically if a Main Camera exists.
- **Coord Mode (`_CoordMode`)**:
  - `0 = UV`: pattern follows the object’s UVs (best for a single background quad).
  - `1 = Object`: pattern sticks to the object’s local XY (can help avoid UV seam issues).
  - `2 = World`: pattern sticks to world XY (objects moving through it will appear to “slide” under the pattern).
- **Tiling (`_Tiling`)**: Repeats the pattern across X/Y.
  - Larger tiling = more repeats (more “busy”).
- **World Coord Scale (`_WorldCoordScale`)**: Only matters in World mode.
  - Bigger = pattern changes faster as you move in world space.
- **Use Object Space (legacy) (`_UseObjectSpace`)**: Older toggle; if set to 1, it forces Object mode.

### Animation

- **Time override (`_T`)**: If `0`, the shader uses Unity time automatically.
  - If you set `_T` yourself (the controller does), you get deterministic playback.
- **Evolve Speed (`_Speed`)**: How quickly the pattern “morphs” over time.
  - `0` = frozen pattern.
- **Scroll Direction (`_ScrollDir`)**: Direction the pattern slides (X/Y).
- **Scroll Speed (`_ScrollSpeed`)**: How fast it slides in that direction.

### Noise

- **Seed (`_Seed`)**: Changes the random layout.
  - Different seed = different arrangement, same “style”.
- **Scale (`_Scale`)**: Size of blobs/features.
  - Bigger scale = bigger, smoother blobs.
  - Smaller scale = tighter, noisier detail.
- **Octaves (`_Octaves`, 1..6)**: Adds layers of detail.
  - More octaves = richer detail, but can look grainier.
- **Lacunarity (`_Lacunarity`)**: How quickly each detail layer gets “smaller”.
  - Higher = detail layers shrink faster (more high-frequency detail).
- **Gain (`_Gain`)**: How strong each added detail layer is.
  - Higher = more visible detail layers.

### Domain Warp (adds “turbulence”)

- **Warp Enabled (`_WarpEnabled`)**: Turn warping on/off.
- **Warp Seed (`_WarpSeed`)**: Random layout for the warp itself.
- **Warp Scale (`_WarpScale`)**: Size of the warp shapes.
- **Warp Amplitude (`_WarpAmp`)**: Strength of the warp (how “swirly” / “liquidy” it gets).
- **Warp Octaves (`_WarpOctaves`, 1..4)**: Detail inside the warp.

### Swirl / Spin (twist the whole image around center)

- **Swirl Enabled (`_SwirlEnabled`)**: Enables a center-based twist.
- **Swirl Degrees (`_SwirlDegrees`)**: Twist amount at the outer edge.
  - Positive/negative flips direction.
- **Swirl Falloff (`_SwirlFalloff`)**: Where the twist happens.
  - `1` ≈ evenly across the image
  - Higher = mostly near the edges
- **Spin Enabled (`_SpinEnabled`)**: Rotates the entire sampling window over time.
- **Spin Deg/Sec (`_SpinDegPerSec`)**: Rotation speed.

### Stylization (final “grading”)

- **Contrast (`_Contrast`)**: Pushes values away from mid-gray.
  - Higher contrast = bolder separation.
- **Brightness (`_Brightness`)**: Shifts the output lighter/darker.

### Palette banding (color bands)

- **Use Palette (`_UsePalette`)**:
  - `1` = 4-band palette look (stylized)
  - `0` = grayscale
- **Palette 0..3 (`_Palette0`..`_Palette3`)**: The 4 colors used for the bands.
  - Changing these is the fastest way to restyle the background.

### Extra parameters in the Lit version

On `Noise/BackgroundGPU Lit`, you also get:

- **Base Color**: Multiplies the noise color (overall tint).
- **Metallic / Smoothness**: Standard material sliders.
- **Emission Color / Strength**: Makes it glow even in dark scenes.

---

## Example outputs (where to see them)

### Included example materials (quick)

Try these first:

- `Assets/Shaders/Example Materials/M_NoiseBG.mat`
- `Assets/Shaders/Example Materials/M_NoiseBG 1.mat`

## Known limitations (what it does not do yet / where it breaks)

- **URP-only**: the shaders are tagged for the Universal Render Pipeline. They won’t work as-is in HDRP/Built-in without changes.
- **Palette is fixed to 4 bands on GPU**: presets can contain more colors, but the GPU shader only uses **Palette 0..3** (4 total).
- **Not “real” 3D noise**: the animation is made by sliding a 2D pattern over time (it looks good, but it’s not the same as true 3D volumetric noise).
- **Octave limits**:
  - Base noise: max **6** octaves (`_Octaves`)
  - Warp: max **4** octaves (`_WarpOctaves`)
- **Render order pitfalls**:
  - Backgrounds can be hidden if they render at the wrong time.
  - These shaders are set up to render **after** the skybox but **before** most geometry (queue `Geometry-100`). If you change render queues or camera settings, it can disappear behind/over things.
- **Coord modes can “swim”**:
  - World mode intentionally sticks to the world, so if your camera moves you’ll see the pattern slide.
  - UV mode depends on mesh UVs; weird UVs can cause stretching.
- **GPU controller maps only a subset of CPU settings**:
  - The `NoiseBackgroundSettings` asset is more fully used by the CPU generator (`NoiseBackgroundGenerator`).
  - The GPU shader has extra knobs like `_CoordMode`, `_Tiling`, and `_WorldCoordScale` that aren’t part of the preset.
- **Lit shader is intentionally simple**:
  - It’s mainly diffuse + emission so it stays stable as a “background.” It’s not trying to be a physically perfect surface.
