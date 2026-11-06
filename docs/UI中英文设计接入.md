# 正式游戏 UI 中英文设计接入 · 2026-09-19

当日修复：用户指出英文选人页文字错乱。已将标题从点击按钮的相对布局改为照片白边上的独立世界坐标图层，逐人校正大小、位置和旋转；简介改为紧裁剪、等比例显示，避免压扁。原 PNG 和中文图不变。上中下镜头及 1440/844 宽度验证见 `character-english-layout-qa.json`，截图为 `character-english-after-en-top.png` / `character-english-after-en-bottom.png`。23 项测试和构建再次通过。

主菜单右下角及设置页左侧纸签可切换中文／English UI。默认中文；偏好独立保存，切换不刷新、不重建当前游戏。`?lang=en` / `?lang=zh` 可临时指定初始语言。

追加官网展示版本中用户要求同步的四份 PNG：书法标题、五个主菜单按钮、选人标题与董点事简介、三个人物手写档案。保留原纸张、印章、人物照片、雾层和胶片效果。原中文 PNG 全部保留，英文图片成功解码后才替换对应图层；英文档案加载失败时显示可读的英文 HTML 档案。

来源：`E:\抖音开发者工具\PNG_UI_HTML复刻\000 公司官网\games\dongbei-disco`。资产位于正式项目既有 `public/assets`，四份 PNG 与来源及 `dist/assets` 逐字节一致；证据见 `ui-language-asset-audit.json`。逻辑入口为 `src/core/ui-language.js`，样式为 `ui-language.css`，界面词条为 `ui-language-copy.js`。没有复制官网打包的玩法代码。

范围：主菜单、选人、设置、存档、音乐库、人物档案等界面。车站剧情正文、单行阅读器、故事选项及笔记内容仍为原中文，不能作为整款游戏英文剧情完成的证明。报纸、奖状、结婚证等美术道具保留其原中文。

验证：23 项 Node 测试通过，Vite 构建通过。隔离内存存档、静音、后台 Google Chrome 直接 CDP 检查英文 PNG、三人档案、中文还原、开始进入 A000、反复切换不改存档与设置、继续游戏；详见 `ui-language-qa.json`。未重启或直接检查 Electron 工作台，实际美感仍待用户评价。

另以隔离测试页预存英文偏好和自动档，验证 1440/844 宽度的五按钮菜单；主动阻断英文档案 PNG 后确认英文 HTML 可读，零运行异常。证据：`ui-language-fallback-qa.json`；截图：`ui-language-en-continue.png`、`ui-language-en-main-844.png`、`ui-language-en-dossier-fallback.png`。

校验限制：`npm run check` 中的旧迁移哈希检查发现 10 份 INK 文件与迁移清单不同，其余 162 份一致；这些剧情文件先前已有修改，本次没有改动剧情或把新哈希冒充原迁移哈希。当前 HTML/CSS 路径与新增 PNG 另行检查通过。详情在 `ui-language-asset-audit.json`。

改前入口备份：`.local/index.html.before-ui-language-20260919`、`.local/scene.js.before-ui-language-20260919`。语言偏好键为 `northeast-disco.ui-language.v1`，不与剧情存档或音量设置共用。

2026-09-20：用户要求英文缩到三分之二，本轮按选人页三个英文标题及简介执行（页面范围未另获确认）。等比例缩放，中心与旋转保持；Chrome CDP实测宽高比0.666667、中心偏移0，1440/844双语检查及构建通过。证据 character-english-scale-20260920-qa.json；人工美感评价待定。本地Steam英文选人截图同步更新，旧图保留。
