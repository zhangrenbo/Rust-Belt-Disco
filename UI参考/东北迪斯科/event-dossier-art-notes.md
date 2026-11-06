# 主页面右侧档案袋

## 2026-09-09 首页左移与铅笔字

- 档案袋整体向左移动约 100px，当前定位为 `left:calc(56.2% - min(100px, 10cqw))`。桌面宽度 1440/1920 时移动 100px；较小舞台按比例收敛位移。原有尺寸、角度、纸张 PNG 与滤镜保持。
- 原来的单张图片改为 `.event-dossier-bag` 容器，内部原图 `.event-dossier-paper` 与独立字层 `.event-dossier-handwriting` 一起移动。
- 新字层：`assets/event-dossier-handwriting-xiongshou-v1.png`，1536 × 1024，内置 imagegen 生成；参考原袋面，仅生成“凶手”二字，不重新生成档案袋。
- 字迹放在原有“档案”印字下方的表格区域，顺袋面旋转 -8°；使用 multiply 叠合、0.38 不透明度。笔画有颗粒、断裂、擦除和淡灰涂抹痕迹。
- 新 PNG 的真实 A 通道已检查：alpha 范围 0–253，全透明像素 1,231,261，半透明像素 341,603，四角均 alpha=0。生成结果直接复制，未进行抠图或二次栅格编辑。
- 最终通过固定工作 Chrome 9223 的直接 CDP 连接检查 1440 × 810、1920 × 1080、844 × 390：袋面和字层加载正常、无溢出、菜单可点击、仅首页出现、无捕获到的运行时异常。已查看最终桌面与小屏截图：`_qa_dossier_pencil_1440.png`、`_qa_dossier_pencil_844.png`；另保存 `_qa_dossier_pencil_1920.png`。
- 以下原始 v1 记录中的定位与单张 img 结构已由本节更新。

### 铅笔字层的实际提示词（内置 imagegen）

```text
Use case: compositing.
Asset type: ONE separate transparent PNG handwriting decal for an existing Chinese archival-envelope game prop.
Input image 1 is a material and pose reference only: the existing aged brown paper envelope will remain completely unchanged in the HTML. Create ONLY an isolated handwritten inscription to be layered on the paper.
Text (verbatim): "凶手". Exactly these two simplified Chinese characters in a single horizontal line, 凶 on the left and 手 on the right. Correct Chinese structure but casual uneven human handwriting using a thin graphite pencil, never a printed typeface, ink, brush or calligraphic display lettering.
The writing has been severely rubbed and partially erased over many years: dry granular graphite, uneven pressure, many missing fragments along strokes, faint rubbed graphite traces beside the strokes, some almost vanished sections. Overall faded dark warm gray, deliberately difficult to read at first glance while still identifiable on close inspection. Do not make bold solid black strokes. Keep every stroke physically believable as a pencil line, no extra scribbles, no circles, no underlines, no additional characters.
Composition: two characters centered with a small comfortable gap on a landscape canvas approximately 3:2, writing occupying about 70% of canvas width and 45% height, modest transparent margins. Straight baseline with individual characters subtly imperfect; CSS will align the decal with the envelope afterward.
CRITICAL: output ONLY the pencil marks and faint smears, genuinely transparent RGBA alpha=0 in all empty areas, subtle partial alpha in rubbed graphite. NO envelope, NO paper texture, NO colored rectangle, NO background, NO painted checkerboard. Preserve actual alpha transparency.
```

- 修改范围：`index.html` 中 `.view-mainmenu` 的桌面，在指认的 `img.event-desk` 后添加独立 `img.event-dossier-bag`。
- 成品：`assets/event-dossier-bag-v1.png`，1122 × 1402，RGBA。
- 美术生成：内置 imagegen，以 `assets/event-desk-bg-v2.png` 为画风与光照参考。
- 原始生成图：`assets/event-dossier-bag-source-v1.png`。生成器两次返回 RGB 棋盘背景，因此用 `_prepare_event_dossier.ps1` 处理与画布边缘相连的中性灰背景，保留袋内原始像素，生成真实透明通道。
- 透明检查：605009 个全透明像素，3661 个半透明轮廓像素，964374 个不透明像素；四角透明，纸面中心不透明。
- 布局：右侧独立 PNG，完整等比显示，`left:56.2%`、`top:8.4cqw`、`width:34.5cqw`。位于原有桌面光照、暗角和胶片效果之下，附加短距离右下贴桌阴影。原始桌面 PNG 保持原文件。
- 接入样式仅作用于 `.view-mainmenu > .event-dossier-bag`，没有将素材加入其他使用相似桌面的界面。
- 视觉检查：通过固定工作 Chrome 的 9223 端口直接 CDP 连接，复用现有浏览器和配置；仅新建本任务本地预览标签。没有使用旧 QA 脚本里的临时 Chrome 启动流程。
- 1016 × 560、1920 × 1080、844 × 390 检查均通过：图片加载成功、仅主页面有一个档案袋、完整处于舞台内、位于菜单右侧、无裁剪与溢出、菜单点击区域可命中，无捕获到的 JavaScript 异常。另已查看 1440 × 810 页面截图。
- 预览截图：`_qa_event_dossier_1016.png`、`_qa_event_dossier_1440.png`、`_qa_event_dossier_1920.png`、`_qa_event_dossier_844.png`。

## 实际生成提示词

```text
Use case: stylized-concept.
Asset type: ONE standalone transparent PNG prop for an existing game main-menu desk.
Input image 1 is a STYLE AND LIGHTING REFERENCE only: the existing dark olive-green leather desk, aged gray-beige paper, restrained brown/sepia colors and warm overhead illumination. Do not reproduce its background.
Generate one old Chinese kraft-paper document envelope / archival file bag, closed, with its broad paper flap and two small dark brown circular string fasteners joined by a short loop of aged cotton string. Slightly thick from documents inside, realistic folded side seams, subtle rubbed corners, small natural wrinkles and paper fibers. It should unmistakably read as a physical paper archival envelope, not a book, briefcase or web UI panel.
Camera: directly overhead, looking down at the bag lying flat on a horizontal tabletop; almost orthographic, very shallow visible paper edge thickness. Portrait orientation, the long axis tilted gently 8 degrees clockwise within the image. Entire envelope and shadow fully visible with 7-10 percent transparent margin. Output canvas portrait approximately 4:5.
Style: match the reference's detailed painterly archival realism, muted gray-brown kraft paper, desaturated warm taupe, restrained texture and natural small wear. No orange/yellow saturation. Midtone paper, not a brightly lit cream sheet.
Lighting: single broad warm light from screen upper-left (toward the reference's overhead center light), softly illuminating upper-left edge and flap; gradual falloff to dimmer lower-right, subtle cool olive ambient in shaded folds. Include a SHORT soft semitransparent contact shadow close to the lower-right edge, so the envelope reads as resting directly on the desk, never floating. Paper remains opaque.
Cover: only two faded dark reddish-brown Chinese characters "档案" printed modestly in the lower middle, with a very faint simple rectangular form outline and a few EMPTY horizontal rules beneath. No invented handwriting, dates, numbers, logos, other words, photographs, seals or extra props.
CRITICAL OUTPUT: real RGBA PNG with actual transparent alpha outside the single envelope and its subtle contact shadow. Empty transparent background. Do NOT paint a checkerboard, white backdrop, black backdrop, green backdrop or any tabletop. Do not include existing desk, books, scattered papers, light beams or surrounding UI.
```

## 透明背景编辑尝试（未采用）

```text
Use case: background-extraction. Precisely edit the supplied image. Keep the single kraft-paper archival bag pixel-faithful: same silhouette, pose, paper texture, two brown string buttons, cotton string, printing "档案", empty ruled rectangle, colors and lighting. Remove EVERY gray checkerboard square and all background outside the bag. The checkerboard in this input is erroneous painted background, not transparency. Output a PNG with a genuine ALPHA channel, alpha=0 for ALL exterior background, alpha=255 within the opaque envelope, antialiased alpha only at the contour and subtle short contact shadow. This is an isolated asset, not a mockup of a transparent image. DO NOT paint a checkerboard, colored backdrop, ground or new content. Preserve exact full canvas size and object placement.
```
