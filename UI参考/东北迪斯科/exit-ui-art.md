# 退出游戏弹窗美术升级（2026-09-08，已完成）

## 已接入素材

- 新生成纸卡：`exit-dialog-panel-v1.png`，1484 × 927，RGBA。
- 内置 imagegen 以现有设置界面为材质参考生成，`_prepare_exit_panel.py` 处理外部透明背景；纸层、纤维、折痕、夹子、印刷边线与光影来自图片。
- 透明通道检查：152,242 个全透明像素，4,732 个半透明边缘像素，文字区域完全不透明。
- 取消按钮复用 `settings-reset-button-v1.png`；确认退出复用 `settings-apply-button-v1.png`。
- 关闭纸签复用 `assets/disco-dialog-close-v1.png`，已有真实透明通道。
- 四张 PNG 均完整显示，不通过 CSS 裁剪参考图取图；已清除退出弹窗原来的 CSS 渐变纸面与伪元素边框。
- 文字与实际操作保持可访问的 HTML，原生 dialog 使用标题和说明关联，初始焦点位于“取消”。
- 修复点击纸卡内部空白误关弹窗的问题；仅外部遮罩点击关闭。
- “确认退出”保留现有行为：结束当前界面会话并返回待机画面。

## 验证

`python _qa_exit.py` 已通过：

- 主菜单打开退出弹窗、默认取消焦点、取消、Esc、关闭纸签、外部遮罩、确认退出。
- 点击纸卡内部空白不误关，确认后回到主页面且不改变地址。
- 1440 × 810、1920 × 1080、844 × 390 下的所有按钮均在视口内，PNG 加载成功，无 JavaScript 运行异常。
- 通过固定工作 Chrome（9223）直接 CDP 测试；启动检查脚本验证准确的进程和持久配置目录。
- 使用后台新建的任务页面；生命周期激活加焦点模拟使可信 Input 事件正常送达，未切换前台标签页。
- 测试完成仅关闭任务创建的页面，工作 Chrome 继续运行；未新建临时浏览器配置，未重启浏览器。
- 验收截图：`_qa_exit_art_1440.png`、`_qa_exit_art_1920.png`、`_qa_exit_art_844.png`。

## 实际提示词

使用内置 imagegen，参考图：`_qa_settings_dossier_1920.png`。

```text
Use this existing Northeast Disco settings screenshot as the exact material/style reference. Generate ONE independent exit-confirmation dialog PANEL ART asset, isolated, landscape aspect ratio 1.5:1, orthographic straight-on top-down. A compact stack of 2-3 old archival paper sheets: a pale gray-ivory front sheet with tactile paper fibers, subtle old fold creases, slightly worn near-straight edges, thin irregular faded charcoal inset rule around its margins; offset muted manila and dark olive-gray backing sheets visible as a narrow layered edge; ONE small dark worn brass binder clip gripping the upper-left corner. Gentle upper-left light, visible thin paper edge thickness and soft contact shadows make it feel placed on the game's desk. Large calm blank writing area x15%-85%, y20%-82%, for a title, one sentence and two live button controls to be added in HTML. The entire grouped paper panel fills 94% of canvas with enough transparent margin for all edges and shadows. No text, letters, numbers, stamps, icons, close mark, buttons, guns, bullets, photos, keys or other objects. No full UI screenshot or full desktop background. Keep it restrained, realistic and consistent with the reference, not glossy, cartoon, ornate, or heavily distressed. Real transparent RGBA outside the panel; if actual alpha is unavailable use pure black exterior background, never a checkerboard. Render high-resolution standalone PNG.
```

