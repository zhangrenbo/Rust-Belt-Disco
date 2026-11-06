---
name: animation-workflow
description: "Procedural animation pipeline — end-effector-first, exact bone matching, FBX extraction"
metadata: 
  node_type: memory
  type: project
  originSessionId: 4c9d99e4-eb22-4f07-8a77-a62751097e79
---

## Core Principle: End-Effector-First

**Hands and feet are the animation core. Everything else (spine, pelvis, head) follows from limb positions.**

## Pipeline

### 1. Extract Real Skeleton from FBX
- Script: `Assets/Docs/extract_bones.py` — parses binary FBX, outputs `bones_real.json`
- Extracts: bone names, parent-child hierarchy (Connections), local transforms (Lcl Translation/Rotation)
- This gives us the exact skeleton structure — no guessing bone names or lengths

### 2. Build ROLE Map
Each FBX bone gets an exact anatomical role:
- `leg_u` — upper leg (lThighBend, rThighBend)
- `leg_l` — lower leg (lShin, rShin)
- `foot` — foot (lFoot, rFoot)
- `arm_u` — upper arm (lShldrBend, rShldrBend)
- `arm_f` — forearm (lForearmBend, rForearmBend)
- `hand` — hand (lHand, rHand)
- `spine` — spine chain (abdomenLower through chestUpper)
- `pelvis`, `head`, `neck`, `clavicle`
- `twist` — twist bones (SKIP — never animate)
- `root`, `hip` — structural (SKIP or minimal)

### 3. Animation Function Structure
```
animCurve(boneName, A(=amplitude), sh(=shoulderSway), hp(=hipSway), animName, n(=normalizedTime))
  → returns {rx, ry, rz} in radians
```
- Each animation (Idle, Walk, Run, Dash, Atk1/2/3, Hit, Stun, Airborne, Death) gets its own case
- Within each case, switch on bone `role` (NOT substring matching on name)
- Use `isLeft = name.startsWith('l')` for side detection
- **Hand/foot logic comes FIRST**, then derive limb angles, then body

### 4. Preview Tool
- `Assets/Docs/fbx_viewer.html` — loads FBX via Three.js FBXLoader, applies procedural animation
- Requires local HTTP server: `python3 -m http.server 8080` in Docs/
- `check_page.js` — Puppeteer script for automated testing
- Expose `window.skeleton`, `window.ROLE`, `window.animCurve` for debug via `page.evaluate()`

### 5. Key Files
| File | Purpose |
|------|---------|
| `Editor/AnimationGenerator.cs` | Unity Editor tool — generates AnimationClips + AnimatorControllers |
| `Docs/bones_real.json` | Extracted FBX skeleton data (280 bones) |
| `Docs/extract_bones.py` | FBX binary parser |
| `Docs/fbx_viewer.html` | Browser-based animation preview |
| `Docs/default_char.fbx` | Copy of 默认角色.fbx (ASCII filename for HTTP) |

## Character Styles
8 styles defined: Balanced, Elegant, Mysterious, Floaty, Cute, Fierce, Heavy, Swift
Each has: speedMul, amplitudeMul, shoulderSway, hipSway, poseOffset

## Related
- [[hero-rename-getbyindex]] — character data
- [[skill-subsystems]] — ability systems
- [[systems-detail]] — AI systems
