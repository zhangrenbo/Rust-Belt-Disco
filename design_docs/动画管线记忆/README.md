# 3D 模型 · 自动本地生成动画 记忆（从「霓虹天际」项目复制）

来源：`霓虹天际 / E:\霓虹天际\Assets` 的项目记忆，2026-07-03 复制。
这三份是关于 **3D 模型骨骼 + 本地自动生成/程序化动画** 的完整逻辑，供本项目参考复用。

| 文件 | 内容 |
|------|------|
| [ai-animation-tools.md](ai-animation-tools.md) | **本地 AI 生成动画**：ComfyUI-HyMotion (HY-Motion-1.0-Lite) 文本→SMPL-X FBX 本地跑通；工具选型 tier list；批量 50 个 ARPG 动画一次生成的脚本与清单 |
| [animation-workflow.md](animation-workflow.md) | **程序化动画管线**：末端优先原则、从 FBX 提取真实骨骼、ROLE 骨骼映射、animCurve 函数结构、Unity AnimationGenerator |
| [web-animation-viewer.md](web-animation-viewer.md) | **网页动画查看器** fbx_viewer.html：FBX→glb (GLTFLoader 才能蒙皮 Daz G8F)、G8F/Mixamo 两套骨骼、manifest 自动化、踩坑记录 |

## 一句话流程
本地 ComfyUI-HyMotion 文本生成 SMPL-X FBX → retarget 到 G8F 骨骼 → fbx2gltf 转 glb → 网页 fbx_viewer.html 查看评分；程序化动画则走末端优先 + FBX 骨骼提取 + Unity 生成 AnimationClip。

> 注：这些是「霓虹天际」的点时观察，文中 `Assets/Docs/…`、`D:\AI_Models\…` 等路径均指向霓虹天际项目/本机环境，非本项目路径。
