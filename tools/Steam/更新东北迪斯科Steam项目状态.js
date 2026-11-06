const fs = require('node:fs');
const path = require('node:path');

const registryPath = 'E:\\个人和公司信息\\Steam\\Steam项目管理器\\data\\steam_projects.json';
const steamFolder = 'E:\\个人和公司信息\\Steam\\游戏项目\\东北迪斯科';
const candidateFolder = path.join(steamFolder, 'Steam页面素材', '胶囊图与商店图片', '源美术', '候选稿');
const now = new Date().toISOString();
const assetTypes = ['header_capsule', 'small_capsule', 'main_capsule', 'vertical_capsule', 'library_capsule', 'library_hero', 'library_logo', 'library_header', 'shortcut_icon', 'app_icon'];

const registry = JSON.parse(fs.readFileSync(registryPath, 'utf8').replace(/^\uFEFF/, ''));
const languages = registry.settings.languages;
let project = registry.projects.find(item => item.id === 'dongbei-disco');
if (!project) {
  project = { id: 'dongbei-disco', name: '东北迪斯科', createdAt: now };
  registry.projects.push(project);
}

Object.assign(project, {
  name: '东北迪斯科', aliases: [], status: 'preparing', priority: 'high', owner: '',
  projectPath: 'E:\\东北迪斯科', steamFolder, steamAppId: '', steamUrl: '', targetRelease: '',
  nextAction: '人工选择首批无字主视觉母版；确认后完成 Logo 组合、Steam 安全区裁切和尺寸成品。',
  blockers: '实机截图按要求暂空；正式 Logo、商店文案和 31 语本地化资产均未完成人工验收。',
  notes: '2026-09-01：已参考《数学的防御协议》的母版→裁切→本地化标题流程，生成 4 张《东北迪斯科》原创候选；均为待审核草稿，不能视为可上传成品。',
  updatedAt: now, lastCheckedAt: now,
  screenshots: { requiredCount: Number(registry.settings.screenshotMinimum || 5), files: [], checked: false, note: '用户要求截图先空着；严禁用生成概念图冒充实机截图。' },
  logo: { files: [path.join(candidateFolder, '现有书法Logo参考_待审核.png')], checked: false, note: '已登记现有书法 Logo 参考，但尚未检查透明度、可读性和 Steam 尺寸适配。' }
});

project.copy = project.copy || { languages: {} };
project.copy.languages = project.copy.languages || {};
project.steamAssets = project.steamAssets || {};

for (const language of languages) {
  project.copy.languages[language.id] = {
    shortDescription: project.copy.languages[language.id]?.shortDescription || '',
    longDescription: project.copy.languages[language.id]?.longDescription || '',
    checked: false
  };
  for (const assetType of assetTypes) {
    project.steamAssets[assetType] = project.steamAssets[assetType] || { languages: {} };
    const previous = project.steamAssets[assetType].languages?.[language.id] || {};
    project.steamAssets[assetType].languages = project.steamAssets[assetType].languages || {};
    project.steamAssets[assetType].languages[language.id] = { status: previous.status || 'missing', path: previous.path || '', note: previous.note || '' };
  }

  project.steamAssets.main_capsule.languages[language.id] = {
    status: 'draft', path: path.join(candidateFolder, 'A01_返乡车站_横版母版候选.png'),
    note: '无字横版母版候选；尚未裁切到 1232×706、叠加本地化标题或人工检查。'
  };
  project.steamAssets.vertical_capsule.languages[language.id] = {
    status: 'draft', path: path.join(candidateFolder, 'B02_便利店废墟_竖版母版候选_油画化.png'),
    note: '无字竖版母版候选；尚未裁切到 748×896、叠加本地化标题或人工检查。'
  };
  project.steamAssets.library_hero.languages[language.id] = {
    status: 'draft', path: path.join(candidateFolder, 'C01_小镇交错路径_库主视觉母版候选.png'),
    note: '无字库主视觉候选；尚未放大/裁切到 3840×1240 或人工检查。'
  };
  project.steamAssets.library_logo.languages[language.id] = {
    status: 'draft', path: path.join(candidateFolder, '现有书法Logo参考_待审核.png'),
    note: '现有书法 Logo 参考；尚未完成语言方案、尺寸适配与人工检查。'
  };
}

registry.updatedAt = now;
const temporary = `${registryPath}.${process.pid}.tmp`;
fs.writeFileSync(temporary, `${JSON.stringify(registry, null, 2)}\n`, 'utf8');
fs.copyFileSync(temporary, registryPath);
fs.rmSync(temporary);
process.stdout.write(JSON.stringify({ project: project.name, languages: languages.length, screenshots: project.screenshots.files.length, status: project.status }));
