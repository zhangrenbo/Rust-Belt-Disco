# 设置 UI：截图参考版（已完成）

## 参考与结果

- 当前唯一视觉参考：`ScreenShot_2026-09-06_155941_673.png`，已从桌面复制到项目根目录。
- 已按参考重做灰白档案纸、牛皮纸封套、左侧纸签、右侧旧钥匙和右下照片的构图；根据最新要求，已移除左下角的枪与右上角的两颗子弹，并补回遮挡处的纸面。
- 设置页已移除上一版胶片条、外框和右侧导航。左侧两张纸签作为“返回”和“总入口”。
- 所有设置与操作按钮均是 HTML 控件，仍位于同一个 `index.html`。
- 最终素材：`settings-dossier-art-v3.png`，1672 × 941，RGBA，直接存放于参考截图所在目录。
- 素材通过内置 imagegen 编辑生成；`_prepare_settings_dossier.py` 仅移除与画布边缘相连的黑色背景，保留内部物体、纸张纹理和光感。
- 透明通道实测：151,340 个全透明像素，5,992 个半透明边缘像素，中央纸面完全不透明。
- 通过完整的 img 元素显示独立素材，没有 CSS 裁剪、遮罩或定位取图。

## 验证

执行 `python _qa_settings.py`，原生 CDP 检查通过：

- 三次重新加载后首次打开设置均保持停留，不改地址、不出现翻页遮罩。
- 音量滑块、剧情开关、保存后重新加载、恢复默认、Esc 返回、返回原入口均正常。
- 旧胶片条、档案外框和右侧导航不存在于设置界面。
- 1440 × 810、1920 × 1080、844 × 390 下控件均在视口内，无页面滚动溢出；未发现 JavaScript 运行异常。
- 已人工查看页面截图，核对纸签点击位置、中央控件和纸面边界。
- QA 使用项目专用无头 Chrome 的原生 CDP，结束后关闭该实例；不连接或重启用户 Edge。

截图：`_qa_settings_dossier_1440.png`、`_qa_settings_dossier_1920.png`、`_qa_settings_dossier_844.png`。

## 实际图片编辑提示词

方式：内置 imagegen，输入为项目根目录的参考截图。

```text
Edit the supplied reference into a high-resolution standalone game settings UI BACKDROP asset, landscape 16:9. Follow this exact composition closely: one large blank desaturated gray-white sheet on layered slightly rotated archival papers, backed by a worn manila envelope whose open V-shaped lip crosses the bottom; two small blank paper tabs stick out at the upper left; two brass rifle cartridges rest across the top right; an old key at far right; a dark firearm and binder clip partly visible at bottom left; overlapping faded ID photographs with an indistinct anonymous silhouette at bottom right. Match the reference's subdued archival realism, paper fibers, soft overhead light, cool gray shadows, restrained olive/brass accents and age wear. Large unobstructed blank central writing area, x 19%-77%, y 17%-75%, for live HTML settings controls. Keep all props at the margins. Do not add any UI controls, text, numbers, English phrases, logos, right navigation tabs, film strips or rectangular outer frame. The main paper's right edge is nearly straight with small natural wear, no large repeated zigzags. Entire grouped stationery assembly fully visible, near canvas edges. Output isolated RGBA PNG with transparent pixels outside the assembly. If actual alpha cannot be output, put the assembly over perfectly uniform vivid magenta #FF00FF so it can be cleanly keyed; never paint a checkerboard. Only one asset, not a grid. Preserve the reference layout and its photographic material character.
```


## 移除枪与子弹：实际编辑提示词

内置 imagegen；输入为 settings-dossier-art-v2.png，生成结果已接入为 settings-dossier-art-v3.png。

```text
Precise removal edit of this existing game UI backdrop. Remove ONLY (1) the entire handgun at bottom left, including grip, trigger guard, slide and barrel, and (2) both brass cartridges/bullets at the upper right, including their cast shadows. Naturally reconstruct the aged manila envelope, paper stack and dark backing that were behind these removed objects. Keep the bottom-left black binder clip present, resting on the envelope. Keep the key at far right, magnifying glass and photographs at bottom right, left two blank paper tabs, sheet outlines, blank central paper area, paper fibers, creases, illumination, colors, camera and ALL object positions unchanged. Exact same landscape canvas and scale. Add nothing else. No text, no controls. Preserve real transparent RGBA background outside the stationery assembly; if output is RGB use pure black outside the assembly, never a checkerboard. This is a minimal object removal, not a redesign.
```

## 两个操作按钮的图生图升级（已完成）

- 使用内置 imagegen 分别生成两张独立按钮素材，再用 `_prepare_settings_buttons.py` 处理真实透明边缘。
- 恢复默认：`settings-reset-button-v1.png`，1672 × 684，RGBA；26,786 个全透明像素、4,621 个半透明边缘像素。
- 应用设置：`settings-apply-button-v1.png`，1694 × 770，RGBA；31,741 个全透明像素、4,869 个半透明边缘像素。
- 完整显示 PNG，按钮文字保留为 HTML；增加纸边厚度、旧化纹理、投影，以及悬停、按下和禁用反馈。
- 已接入 `index.html` / `app.css`，原生 CDP 验证两张图片加载、恢复默认、保存后重载以及三种尺寸布局均通过。

### reset

```text
Image-to-image redesign of ONE button from this game settings screenshot. Use its aged archival paper and restrained desaturated material style as the reference. Output ONLY ONE isolated horizontal button background art asset, 3:1 aspect ratio, straight rectangular silhouette with tiny natural edge wear, perfectly front-facing orthographic top-down, no perspective or rotation. The whole button fills 94% of the canvas with a narrow margin for its soft contact shadow. Tactile dense paper/card stock, detailed subtle fibers, believable thin layered edge thickness, a fine pressed inset border, gentle upper-left light, restrained lower-right shadow, clean empty center reserved for four Chinese characters rendered later in HTML. All detail must still read at 180 x 60 display pixels. Do not include text, glyphs, symbols, icons, screws, rivets, wax seals, surrounding UI, desktop or other objects. Avoid shiny plastic, modern web gradients, ornate fantasy decoration, oversized rips. True transparent alpha outside the button; if actual transparency is unavailable use pure black background, never a checkerboard. This is the SECONDARY 'restore defaults' button. Warm gray-beige worn archival paper, slightly darker olive-gray pressed border, lightly rubbed edges, two very subtle corner creases. The central face stays pale and fairly even for dark text. Refined physical miniature paper label.
```

### apply

```text
Image-to-image redesign of ONE button from this game settings screenshot. Use its aged archival paper and restrained desaturated material style as the reference. Output ONLY ONE isolated horizontal button background art asset, 3:1 aspect ratio, straight rectangular silhouette with tiny natural edge wear, perfectly front-facing orthographic top-down, no perspective or rotation. The whole button fills 94% of the canvas with a narrow margin for its soft contact shadow. Tactile dense paper/card stock, detailed subtle fibers, believable thin layered edge thickness, a fine pressed inset border, gentle upper-left light, restrained lower-right shadow, clean empty center reserved for four Chinese characters rendered later in HTML. All detail must still read at 180 x 60 display pixels. Do not include text, glyphs, symbols, icons, screws, rivets, wax seals, surrounding UI, desktop or other objects. Avoid shiny plastic, modern web gradients, ornate fantasy decoration, oversized rips. True transparent alpha outside the button; if actual transparency is unavailable use pure black background, never a checkerboard. This is the PRIMARY 'apply settings' button. Deep muted oxblood-brown pigment pressed into thick archival card stock, softly rubbed lighter brown outer edges, fine faded warm-ivory inset border, minute worn specks near margins. The central face stays dark and even for ivory text. Refined physical ink-dyed miniature paper label, not glossy leather or a metal plaque.
```

