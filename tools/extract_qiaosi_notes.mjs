// 把 ink 剧情文件中混入的「巧思」设计笔记块迁出到 design_docs/巧思笔记/
// 用法：node tools/extract_qiaosi_notes.mjs
import { readFileSync, writeFileSync, mkdirSync, readdirSync, copyFileSync, statSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const INK_DIR = join(ROOT, 'content/story/ink');
const DOC_DIR = join(ROOT, 'design_docs/巧思笔记');
const BACKUP_DIR = join(ROOT, '.local/backup_ink_20260917');
const MANIFEST_PATH = join(ROOT, '.local/qiaosi_migration.json');

const FILES = [
  'D0_序章.ink', 'D1_第一天.ink', 'D2_第二天.ink', 'D3_第三天.ink',
  'D4_第四天.ink', 'D5_第五天.ink', 'D6_第六天.ink', 'D7_第七天.ink',
  '最终审判.ink', '背景设定.ink',
];

// 尾部是 ink 导航行，设计文档里无意义，剥离
const NAV_TAIL = /^(?:- \(返回\)|\+ \[返回目录\]|\s*-> [A-Za-z0-9_]+_main)$/;

function parseEntries(menuLines) {
  const entries = [];
  let i = 0;
  while (i < menuLines.length) {
    const labelLine = menuLines[i];
    if (!/^\+ \[/.test(labelLine)) { i++; continue; }
    let targetLine = -1;
    for (let j = i + 1; j < menuLines.length; j++) {
      if (/^\+ \[/.test(menuLines[j])) break; // 下一个条目，没找到跳转
      if (/^\s*->\s+/.test(menuLines[j])) { targetLine = j; break; }
    }
    if (targetLine < 0) { i++; continue; }
    const m = menuLines[targetLine].match(/^\s*->\s+(\S+)/);
    entries.push({ label: labelLine, labelIdx: i, targetIdx: targetLine, target: m ? m[1] : '' });
    i = targetLine + 1;
  }
  return entries;
}

// 备份（无 git，必须留后路；递归复制逐文件做，避免 cpSync 被沙盒拦截）
if (!existsSync(BACKUP_DIR)) {
  mkdirSync(BACKUP_DIR, { recursive: true });
  for (const f of readdirSync(INK_DIR)) {
    if (statSync(join(INK_DIR, f)).isFile()) copyFileSync(join(INK_DIR, f), join(BACKUP_DIR, f));
  }
}

mkdirSync(DOC_DIR, { recursive: true });
const manifest = { migrated_at: '2026-09-17', files: [] };
const indexRows = [];

for (const name of FILES) {
  const path = join(INK_DIR, name);
  const text = readFileSync(path, 'utf8').replace(/\r\n/g, '\n');
  let lines = text.split('\n');

  // 按 === xxx === 切块
  const sections = []; // {header, start, end}
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/^=== (.+?) ===$/);
    if (m) sections.push({ header: m[1], start: i, end: lines.length - 1 });
  }
  for (let s = 0; s < sections.length - 1; s++) sections[s].end = sections[s + 1].start - 1;
  const byHeader = new Map(sections.map(s => [s.header, s]));

  const prefix = name.startsWith('最终审判') ? 'FT' : name.startsWith('背景设定') ? 'BG' : 'D' + name.match(/^D(\d)/)[1];
  const mainHeader = `${prefix}_main`;
  const mainSec = byHeader.get(mainHeader);
  if (!mainSec) throw new Error(`${name}: 找不到 ${mainHeader}`);

  // 解析菜单条目
  const menuLines = lines.slice(mainSec.start, mainSec.end + 1);
  const entries = parseEntries(menuLines);
  const qiaosi = entries.filter(e => e.label.includes('巧思'));

  // 删除菜单中的巧思条目并重编号
  const deleteIdx = new Set();
  for (const e of qiaosi) { deleteIdx.add(e.labelIdx); deleteIdx.add(e.targetIdx); }
  const keptMenu = menuLines.filter((_, i) => !deleteIdx.has(i));
  let n = 0;
  for (let i = 0; i < keptMenu.length; i++) {
    keptMenu[i] = keptMenu[i].replace(/^(\+ \[)\d+\./, (m, p) => `${p}${String(++n).padStart(2, '0')}.`);
  }

  // 提取巧思块内容到 md
  const mdLines = [
    `# 巧思笔记（自 ${name} 迁出）`, '',
    `> 迁移时间：2026-09-17　原文件：\`content/story/ink/${name}\``,
    '> 这些块原混在 ink 剧情文件的目录里，是设计笔记而非剧情，已按约定迁至 design_docs。', '',
  ];
  const removedHeaders = new Set();
  for (const e of qiaosi) {
    const sec = byHeader.get(e.target);
    if (!sec) throw new Error(`${name}: 菜单指向不存在的块 ${e.target}`);
    removedHeaders.add(e.target);
    let block = lines.slice(sec.start + 1, sec.end + 1);
    // 剥离尾部 ink 导航行
    while (block.length && NAV_TAIL.test(block[block.length - 1].trim())) block.pop();
    while (block.length && !block[block.length - 1].trim()) block.pop();
    // 首行即标题（与菜单标签一致）
    let title = e.label.replace(/^\+ \[\d+\. /, '').replace(/\]$/, '');
    const firstNonEmpty = block.findIndex(l => l.trim());
    if (firstNonEmpty >= 0) {
      title = block[firstNonEmpty].trim();
      block = block.slice(firstNonEmpty + 1);
    }
    while (block.length && !block[0].trim()) block.shift();
    mdLines.push(`## ${e.target}　${title}`, '');
    mdLines.push(...block, '');
  }

  // 重建文件：替换菜单 + 删除块（菜单变短后，其后所有行号统一上移 shift 行）
  let newLines = lines.slice(0, mainSec.start).concat(keptMenu, lines.slice(mainSec.end + 1));
  const shift = menuLines.length - keptMenu.length;
  const sorted = sections.filter(s => removedHeaders.has(s.header)).sort((a, b) => a.start - b.start);
  for (const sec of sorted.reverse()) {
    const s = sec.start - shift, e = sec.end - shift;
    newLines.splice(s, e - s + 1);
  }

  // 更新头部计数
  const blockCount = sorted.length; // 移除数
  newLines = newLines.map(l =>
    l.replace(/^(\/\/ 共 )\d+( 个顶层区块)/, (m, p, q) => `${p}${Number(m.match(/\d+/)[0]) - blockCount}${q}`)
      .replace(/(选择区块（共 )\d+( 块）：)/, (m, p, q) => `${p}${n}${q}`));

  // 收尾：文件末尾确保单个换行
  let out = newLines.join('\n').replace(/\n{3,}$/, '\n\n');
  if (!out.endsWith('\n')) out += '\n';

  const docPath = join(DOC_DIR, `${name.replace(/\.ink$/, '')}_巧思笔记.md`);
  writeFileSync(docPath, mdLines.join('\n').replace(/\n{3,}$/g, '\n\n') + '\n', 'utf8');
  writeFileSync(path, out, 'utf8');

  const removed = [...removedHeaders];
  manifest.files.push({ file: name, removed: removed.length, kept: n, targets: removed, doc: docPath });
  indexRows.push(`| ${name} | ${removed.length} | [${name.replace(/\.ink$/, '')}_巧思笔记.md](${name.replace(/\.ink$/, '')}_巧思笔记.md) |`);
  console.log(`${name}: 移除 ${removed.length} 块，保留 ${n} 块 → ${docPath}`);
}

// 索引 README
const index = [
  '# 巧思笔记索引', '',
  '剧情设计阶段的「巧思」笔记块，原混在 `content/story/ink/` 的 ink 文件目录里，2026-09-17 迁出至本目录。',
  'ink 文件现在只包含可玩剧情与设定正文。', '',
  '| 来源 ink 文件 | 迁移块数 | 笔记文件 |', '|---|---|---|',
  ...indexRows, '',
  '## 备注', '',
  '- 每个笔记段落保留原 ink 块 ID（如 D0_0004、BG_0123），可回溯。',
  '- ink 原文备份：`.local/backup_ink_20260917/`。',
  '- 美术管线 `tools/build_mj_story_scene_batch.py` 中以巧思块为出处的场景已改指本目录。',
];
writeFileSync(join(DOC_DIR, 'README.md'), index.join('\n') + '\n', 'utf8');
writeFileSync(MANIFEST_PATH, JSON.stringify(manifest, null, 2) + '\n', 'utf8');
console.log('完成。清单:', MANIFEST_PATH);
