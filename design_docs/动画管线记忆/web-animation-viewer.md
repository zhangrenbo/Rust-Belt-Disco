---
name: web-animation-viewer
description: Web FBX/动画查看器 (Assets/Docs/fbx_viewer.html) — glb 管线、两套骨骼、three.js Daz 坑
metadata: 
  node_type: memory
  type: project
  originSessionId: ab573b48-206d-459a-b85c-968f5ded83f5
---

`Assets/Docs/fbx_viewer.html` 是 three.js 写的网页端动画查看器，用来检查动画资产质量。2026-06-30 重写修好。

**关键坑 — three.js FBXLoader 无法蒙皮 Daz G8F 骨骼**：直接加载 G8F 的 FBX，骨架会动但蒙皮网格停在绑定姿势/飞离（mesh≠skeleton）。Mixamo 的 FBX 反而正常。解决：把所有动画 FBX 用 **fbx2gltf 转成 .glb**（`Assets/Docs/convert.mjs`，输出到 `Docs/glb/`），改用 **GLTFLoader** 加载——glb 蒙皮完美。

**两套骨骼**（用户设定：全项目统一 G8F，Mixamo 是素材待迁移）：
- `Art/Animations/G8F_Dance/` (18) = Daz G8F 骨骼（`hip/pelvis/abdomenLower/lThighBend/lShldrBend`…），和主角 default_char 同骨骼。
- `Characters/Animations/Mixamo/` (32) = `mixamorig:*` 骨骼（Hips/Spine/LeftArm/LeftUpLeg…），异骨骼。
- Mixamo→G8F 的身体骨映射（迁移用）：Hips→hip, Spine→abdomenLower, Spine1→abdomenUpper, Spine2→chestLower, Neck→neckLower, LeftShoulder→lCollar, LeftArm→lShldrBend, LeftForeArm→lForearmBend, LeftUpLeg→lThighBend, LeftLeg→lShin, LeftFoot→lFoot。G8F 把肩/腿拆成 Bend+Twist 两根，Mixamo 单根。

**运行**：`cd Assets/Docs && python -m http.server 8765 --bind 127.0.0.1`，开 `http://127.0.0.1:8765/fbx_viewer.html`（服务是会话级的，关了要重启）。`Docs/G8F_Dance`、`Docs/Mixamo` 是指向真实文件夹的**目录联接**。

**新增动画（2026-07-01 起自动化，免手改 HTML）**：丢 FBX 进文件夹 → `node convert.mjs`（转 glb + **自动写 `manifest.json`**）→ 刷新页面即可。查看器 boot() 时 `fetch('manifest.json')` 拿文件列表（内嵌 `FALLBACK_FILES` 仅离线降级），分组标签按 `glb/<子目录>/` 名自动生成（`Generated/` = ✨ 生成动画，给 HunyuanMotion 等生成产物落点）。已端到端验证：丢进 Generated/ → convert → 列表自动多一组。

**坑 — G8F_Dance 动画 FBX 自带重复骨架（2026-07-01 发现）**：`Art/Animations/G8F_Dance/*.fbx` 转出的 glb 有**两套同名 G8F 骨架**（236节点/2skins，hip+hip_1、pelvis、abdomen、脊柱、颈 全 ×2），GLTFLoader 给第二套加 `_1` 后缀。动画只驱动无后缀那套，`_1` 成孤儿(无数据、偏在一边)，SkeletonHelper 默认会把两套都画 → 看起来"一个角色两条脊椎"。**对比**：主角 `default_char.glb`(同 G8F 骨架) 和 Mixamo 都**无重复**，是 G8F_Dance 这批动画导出时的产物。**查看器修法**(fbx_viewer.html load())：从 `anims[0].tracks` 反查被驱动的骨名→爬到骨架根→`SkeletonHelper(skelRoot)` 只画那一套；dbg 显示真实骨数并标"(源195·含重复骨架)"。源 FBX 本身未清理(若进 Unity 需另处理)。诊断工具：`node inspect_glb.mjs <glb...>`(解析 glb JSON chunk 列节点/skins/重名)。

**查看器功能（2026-07-01 扩充）**：时间轴拖动条+时间显示、逐帧步进(`, .`键/±1帧钮)、循环开关、正/侧/透视相机预设、背景循环、截图导出(PNG下载，renderer 已开 `preserveDrawingBuffer`)。frameStep 从 clip 首轨 times 间隔推断。

**无头验证脚本**（puppeteer 在 Docs/node_modules）：`node verify_viewer.mjs`(主查看器)、`node verify_compare.mjs`(对比页)。关键：headless Chrome 需 `--use-gl=angle --use-angle=swiftshader --enable-unsafe-swiftshader` 才能创建 WebGL 上下文，否则报 "could not create WebGL context"。

**`anim_viewer.html`（SMPL-X vs G8F retarget 对比工具，2026-07-01 修好）**：改相对路径挂 8765（原硬编码 :8080）。从 `bones_real.json`(G8F rest, 280骨, tx/ty/tz/rx/ry/rz/parent) + `transferred_anim.json`(23 动画骨, frame_rotations 是**绝对**局部欧拉度, 已含 hip 的 Z-up→Y-up 修正) 手工搭 G8F 骨架，与 `test_anim.fbx`(SMPL-X源, FBXLoader 正常蒙皮) 同步播放对比。**关键坑**：手搭骨架整体在 Daz **Z-up** 空间(hip 局部 z=1.12 是身高)，three.js 是 Y-up → 必须 `g8fGroup.rotation.x = -PI/2` 整体转正(位置+旋转都在同一 Daz 空间自洽，转容器即可)。加了父子连线(LineSegments 每帧更新世界坐标)、F键开关骨骼。注意 `transferred_anim.json` 的 `metadata.duration_s` 是损坏值(700896000)，用 `frame_count/fps` 算(150/30=5s)。

**生成动画（walk_workflow.json）**：ComfyUI HunyuanMotion 工作流(HY-Motion-1.0-Lite，文本→SMPL-X FBX→retarget→glb)。需 ComfyUI 跑在 `:8188`，本机当前**未安装**，无法运行；test_anim.fbx 是之前生成的产物。

**实现要点**（踩坑记录）：用 `clock.getDelta()` 驱动 mixer；frame() 缩放模型到 ~1.7 高并居中（glb 已是米单位）；剥离根骨 `.position` 轨道让动画**原地播放**（否则 Mixamo 跑出框）；G8F 贴图缺失(Daz 基础贴图没进项目)但 fbx2gltf 仍嵌了肤色。旧版备份 `fbx_viewer_legacy.html.bak`。三种显示模式：完整/线框/仅骨骼。

迁移到 Unity 看动画：选 FBX → Inspector 底部预览窗 ▶；跨骨骼 retarget 需把 rig 改 Humanoid。详见 [icharacterbrain-refactor] 无关，相关角色见项目骨骼 default_char (G8F)。
