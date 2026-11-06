---
name: ai-animation-tools
description: "AI animation tools evaluation — MoCapAnything, ComfyUI-HyMotion, custom pipeline"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 4c9d99e4-eb22-4f07-8a77-a62751097e79
---

## Goal: Video → Genesis 8.1 FBX animation

### Tier List (theoretical best → worst)

1. **MoCapAnything** — Monocular video + rigged 3D asset → BVH animation. Designed for arbitrary skeleton retargeting.
2. **Custom Pipeline** — MediaPipe 33-keypoint → 3D lifting → IK solve → Genesis 8.1 bones. Only targets our skeleton.
3. **ComfyUI-HyMotion** — Text-to-motion, not video-to-motion. Supports DAZ Genesis output. Native FBX export.
4. **AnyTop** — Skeleton topology → motion diffusion. Research paper, limited practical use.
5. **SMooDi** — Stylized motion diffusion. Google Drive models (blocked in China).

### Key Constraints
- Must be free & open source
- Must download without VPN (HuggingFace OK, Google Drive NOT OK)
- Must run locally on Windows
- Must output to Genesis 8.1 skeleton

### ComfyUI-HyMotion Details
- Git: https://github.com/Aero-Ex/ComfyUI-HyMotion
- License: Open source (no explicit license file)
- Models: HuggingFace (accessible) — HY-Motion-1.0 (~4.5GB), Lite (~1.5GB)
- Text encoder: Qwen3-8B (needs 16GB VRAM) or GGUF quantized
- Supports: Mixamo, DAZ G3/G9, UE5 Mannequin, UniRig
- Output: FBX with skeleton

### MoCapAnything — VERDICT: Unusable
- Code: `phongdaot/MocapAnything` (unofficial reimplementation, MIT license)
- **No model weights provided** — no download URLs anywhere
- Requires TripoSG dependency (heavy external repo)
- Shell scripts only (setup.sh, train.sh) — no Windows support
- Conclusion: research codebase, not ready for practical use

### ComfyUI-HyMotion — VERDICT: Working, verified 2026-06-26
- Code: `Aero-Ex/ComfyUI-HyMotion` (open source)
- Models: HuggingFace (accessible without VPN), Lite ~1.5GB, Full ~4.5GB
- Native DAZ Genesis skeleton support
- Text-to-motion (not video-to-motion, but good enough)
- Requires ComfyUI installation
- Qwen3-8B text encoder: use `Qwen3-8B_fp8.safetensors` (8.8GB, safetensors) **NOT** GGUF variant
  - GGUF variant fails due to ComfyUI-GGUF / HyMotion compatibility issues on Windows+aimdo
  - FP8 safetensors loading requires 2 patches to `hymotion/network/text_encoders/text_encoder.py`:
    1. Fallback direct module resolution in key matching (line ~319)
    2. `setattr` fallback when `param is None` (line ~415)

### First Successful Generation (2026-06-26)
- Prompt: "a person walks forward then jumps and waves"
- Output: `test_walk_wave_*.fbx` (16.1 MB)
- Duration: 5s, FPS: 30
- Template: `boy_Rigging_smplx_tex.fbx` (wooden boy model)
- Pipeline nodes: DiT Loader → Text Encoder Loader → Text Encode → Sampler → Export FBX

### Installation Status (2026-06-26)
- ✅ YOLOv8x-pose: `D:\AI_Models\YOLO\yolov8x-pose.pt` (133MB)
- ✅ ComfyUI: `D:\AI_Models\ComfyUI\` (v0.26.0)
- ✅ ComfyUI-HyMotion: `D:\AI_Models\ComfyUI\custom_nodes\ComfyUI-HyMotion\` (cloned, patched)
- ✅ ComfyUI-GGUF: `D:\AI_Models\ComfyUI\custom_nodes\ComfyUI-GGUF\` (cloned, for GGUF support)
- ✅ HY-Motion-1.0-Lite: `models/hymotion/HY-Motion-1.0-Lite/latest.ckpt` (1.84GB)
- ✅ CLIP ViT-L/14: `models/text_encoders/clip-vit-large-patch14.safetensors` (1.71GB)
- ✅ Qwen3-8B FP8: `models/text_encoders/Qwen3-8B_fp8.safetensors` (8.8GB)
- ✅ Qwen3-8B GGUF: `models/text_encoders/Qwen3-8B-Q4_K_M.gguf` (2.0GB, backup)
- ✅ FBX template: `models/hymotion_fbx_templates/boy_Rigging_smplx_tex.fbx` (16.5MB)
- ✅ All Python deps installed (torch, transformers, fbxsdkpy, gguf, etc.)

### ComfyUI Server
- Launch: `cd D:/AI_Models/ComfyUI && python main.py --listen 127.0.0.1 --port 18188`
- Web UI: http://127.0.0.1:18188
- GPU: NVIDIA GeForce RTX 5080 (16GB VRAM)
- Python: 3.14.2, PyTorch: 2.10.0+cu128

### Batch ARPG Animation Generation (2026-07-01)
- **50 个 ARPG 动画**分 12 类一次性生成，50/50 成功 0 失败
- Script: `Assets/Docs/batch_generate.mjs` — Node.js 脚本调 ComfyUI API (:18188)，顺序提交、轮询等待
- 生成耗时：前期 ~10-25s/个，后期 GPU 热了 ~200-360s/个；总约 2 小时
- FBX 输出落点：`D:/AI_Models/ComfyUI/output/hymotion_fbx/` (SMPL-X 骨架)
- 后处理：`Assets/Docs/postprocess_batch.mjs` — 批量复制 FBX→Docs/Generated/ → npx fbx2gltf 转 glb → manifest.json 自动更新
- 也可直接用 `node convert.mjs`（自动扫描 Generated/ 目录）

### 50 ARPG 动画清单（按类别）
| 类别 | 数量 | 前缀 |
|------|------|------|
| Locomotion | 6 | walk_fwd, run_fwd, walk_back, strafe_left/right, crouch_walk |
| Combat Idle | 2 | idle_combat, idle_sword |
| Melee Attacks | 7 | atk_overhead, atk_horizontal, atk_thrust, atk_uppercut, atk_diagonal, atk_spin, atk_smash |
| Combo | 3 | combo_3hit, combo_flurry, combo_spin |
| Ranged/Magic | 6 | cast_projectile, cast_2h_blast, cast_channel, cast_ground, ranged_bow, ranged_throw |
| Defense | 3 | def_block, def_parry, def_shield |
| Dodge/Movement | 5 | dash_fwd, dodge_back, dodge_roll, dodge_left, jump_combat |
| Hit React | 5 | hit_light, hit_heavy, hit_knockback, hit_knockdown, hit_airborne |
| Stun/GetUp | 3 | stun_idle, getup_front, getup_back |
| Death | 2 | death_forward, death_back |
| Skills | 5 | skill_charge, skill_leap, skill_whirlwind, skill_buff, skill_heal |
| Interaction | 3 | pickup_item, draw_weapon, drink_potion |

### 查看器（更新）
- 启动：`cd Assets/Docs && node server.mjs`（自带 API，ratings 自动落盘 ratings.json）
- 打开：http://127.0.0.1:8766/fbx_viewer.html
- 👍👎📌 评分/标记帧自动 POST 到 `/api/ratings` → 写入 `Docs/ratings.json`
- 启动时从 server 加载评分合并到 localStorage（server 数据优先）
- 同步状态：🟢=已落盘 ⚫=server 未运行
- Generated 分组 = ✨ 生成动画，manifest.json 已有 110 个 glb

## Related
- [[animation-workflow]] — current procedural animation pipeline
- [[systems-detail]] — project architecture
- [[web-animation-viewer]] — fbx_viewer.html 详情
