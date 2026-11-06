# 存档 UI 接入记录 · 2026-09-09

用户在工作台指认根目录 `index.html` 的 `section[data-view="entry"]`，确认此处的“档案设置组”页面才是要使用的存档 UI。

## 已接入

- 主菜单存档和车站存档按钮打开 `entry`。旧 `archive` 链接、直接访问 `#archive` 均通过路由别名转到此页。
- 保留现有金属底板、四张 `evidence-ticket-v2.png` 票据和下方两张纸条的位置与样式。它们分别对应手动槽位 01–06，文字显示真实存档状态。
- 有效存档的前四张票据显示当前场景插图，取自 `public/media/station-scene.png`。这不是动态截屏。空白票据保留原 PNG 的 NO DATA 画面。
- 点击槽位后可读取或保存当前旅程；没有旅程时不能保存，空槽位不能读取，损坏存档禁止覆盖并保留原始数据。
- localStorage 的版本和键保持不变，自动存档仍由主菜单“继续游戏”读取。手动存档不受“开始游戏”初始化自动档影响。
- “继续游戏”保持现有透明 PNG，并通过 `translateY(-15px)` 上移 15 CSS px。

实现文件为根目录 `index.html`、`scene.js`、`src/station/controller.js`、`src/station/station.css`。原参考目录不改。修改前备份为 `docs/save-ui-entry-before-20260909.zip`。

## 验证

- `npm run check`：6 项测试通过，包含 768 条剧情组合、HTML 交互流程、空槽位禁用、下方 05/06 槽位保存、损坏存档保留及手动存档读取；172 个迁移文件哈希和 54 个 HTML/CSS 资源路径通过，构建通过。
- 实际页面检查脚本：`scripts/qa-save-entry-cdp.py`。复用固定工作 Chrome CDP 9223，仅在任务创建的页面中使用内存 localStorage，不修改真实浏览器或工作台存档。
- 结果：`docs/save-entry-verification.json`；画面：`docs/save-entry-empty-{896,1440}.png`、`docs/save-entry-saved-{896,1440}.png`，覆盖工作台指认的 896×504 尺寸和 1440×810 尺寸。自动化截图用于实现检查，用户美术验收仍以实际反馈为准。
