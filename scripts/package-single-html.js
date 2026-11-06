// Produce a self-contained file:// build without changing the editable project.
import {readFile, readdir, mkdir, writeFile, stat} from 'node:fs/promises';
import {resolve, relative, dirname, extname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {build} from 'vite';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(process.argv[2] || resolve(root, '.local/东北迪斯科-单文件版.html'));
const publicRoot = resolve(root, 'public'), inventory = new Map(), selected = new Set();
const normalize = value => decodeURIComponent(value).replace(/^\.?\//, '');
const digest = data => createHash('sha256').update(data).digest('hex');
const safeJson = value => JSON.stringify(value).replaceAll('<', '\\u003c');
const mimeTypes = {'.png':'image/png','.svg':'image/svg+xml','.jpg':'image/jpeg','.jpeg':'image/jpeg',
  '.webp':'image/webp','.mp3':'audio/mpeg','.ogg':'audio/ogg','.wav':'audio/wav','.mp4':'video/mp4',
  '.woff':'font/woff','.woff2':'font/woff2','.txt':'text/plain'};
async function scan(directory) {
  for (const entry of await readdir(directory, {withFileTypes:true})) {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) await scan(path);
    else inventory.set(relative(publicRoot, path).replaceAll('\\', '/'), path);
  }
}
await scan(publicRoot);
const paths = [], indexes = new Map();
function include(value) {
  const key = normalize(value);
  if (!inventory.has(key)) throw new Error(`Missing standalone asset: ${value}`);
  selected.add(key);
  if (!indexes.has(key)) { indexes.set(key, paths.length); paths.push(key); }
  return `@@DISCO_ASSET_${indexes.get(key)}@@`;
}
const isExternal = value => /^(?:data:|blob:|#)/.test(value);
function inlineCss(css) {
  if (/@import\b/.test(css)) throw new Error('Resolve CSS imports before packaging');
  return css.replace(/url\(\s*(?:"([^"]*)"|'([^']*)'|([^)]*))\s*\)/g, (whole, a, b, c) => {
    const path = (a ?? b ?? c).trim();
    return isExternal(path) ? whole : `url("${include(path)}")`;
  });
}
const entry = await readFile(resolve(root, 'index.html'), 'utf8');
const stylePaths = [...entry.matchAll(/<link\b[^>]*rel="stylesheet"[^>]*href="([^"]+)"[^>]*>/g)].map(m => m[1]);
const styleSources = await Promise.all(stylePaths.map(path => readFile(resolve(root,path),'utf8')));
const css = styleSources.map(inlineCss).join('\n');
let html = entry.match(/<body[^>]*>([\s\S]*)<\/body>/i)?.[1];
if (!html) throw new Error('Missing document body');
html = html.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, '');
html = html.replace(/\b(src|data-src|poster)="([^"]+)"/g, (whole, attribute, path) =>
  isExternal(path) ? whole : `${attribute}="${include(path)}"`);
html = inlineCss(html);
const sourceHashes = new Map([['index.html', digest(entry)], ...stylePaths.map((path,i) => [path,digest(styleSources[i])])]);
const bundle = await build({configFile:false, root, logLevel:'error', publicDir:false,
  build:{write:false,minify:true,target:'es2022',sourcemap:false,
    lib:{entry:resolve(root,'scene.js'),name:'NortheastDisco',formats:['iife']}},
  plugins:[{name:'standalone-public-assets',enforce:'pre',transform(code,id) {
    if (!id.startsWith(root.replaceAll('\\','/')) && !id.startsWith(root)) return;
    if (!id.endsWith('.js') || id.includes('node_modules')) return;
    sourceHashes.set(relative(root,id).replaceAll('\\','/'),digest(code));
    return code.replace(/(["'`])((?:\/?assets\/|\/?media\/|settings-|exit-dialog-)[^\r\n]*?)\1/g, (whole, quote, path) => {
      if (quote === '`' && path.includes('${')) {
        const pattern = normalize(path).split(/\$\{[^}]+\}/).map(s=>s.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')).join('[^/]+');
        const matcher = new RegExp(`^${pattern}$`);
        const matches = [...inventory.keys()].filter(key=>matcher.test(key));
        if (!matches.length) throw new Error(`Unresolved dynamic asset: ${path}`);
        matches.forEach(include);
      } else {
        if (!/\.(png|svg|jpg|jpeg|webp|mp3|ogg|wav|mp4|woff2?)$/i.test(path)) return whole;
        include(path);
      }
      return `globalThis.__discoAsset(${whole})`;
    });
  }}]});
const chunks = (Array.isArray(bundle) ? bundle : [bundle]).flatMap(result=>result.output).filter(item=>item.type==='chunk');
if (chunks.length !== 1 || chunks[0].imports.length || chunks[0].dynamicImports.length) throw new Error('Bundle still has external modules');
const code = chunks[0].code;
// Include the source notices in the artifact, without adding interface labels.
for (const key of inventory.keys()) if (/license|credit/i.test(key) && key.endsWith('.txt')) include(key);
const embedded = [], assets = [];
for (const path of [...selected].sort()) {
  const bytes = await readFile(inventory.get(path));
  const mime = mimeTypes[extname(path).toLowerCase()];
  if (!mime) throw new Error(`Unknown MIME type: ${path}`);
  if (mime === 'image/svg+xml' && /(?:href\s*=\s*["'](?!#|data:)|url\(\s*["']?(?!#|data:))/i.test(bytes.toString('utf8'))) {
    // SVGs in this project are standalone masks. Fail instead of shipping a hidden external dependency.
    throw new Error(`Check nested SVG dependencies: ${path}`);
  }
  const attribute = path.replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');
  embedded.push(`<script type="application/octet-stream" data-file="${attribute}" data-mime="${mime}">${bytes.toString('base64')}</script>`);
  assets.push({path,bytes:bytes.length,sha256:digest(bytes)});
}
const boot = `(() => {
  const urls = new Map();
  for (const node of document.querySelectorAll('script[data-file]')) {
    const base64 = node.textContent.trim(), parts = [];
    for (let i=0;i<base64.length;i+=65536) {
      const binary=atob(base64.slice(i,i+65536)), bytes=new Uint8Array(binary.length);
      for (let j=0;j<binary.length;j++) bytes[j]=binary.charCodeAt(j);
      parts.push(bytes);
    }
    urls.set(node.dataset.file,URL.createObjectURL(new Blob(parts,{type:node.dataset.mime})));
    node.remove();
  }
  globalThis.__discoAsset = value => {
    const key=decodeURIComponent(value).replace(/^\\.?\\//,'');
    if (!urls.has(key)) throw new Error('内嵌素材缺失：'+key);
    return urls.get(key);
  };
  const node=document.getElementById('disco-document'), packed=JSON.parse(node.textContent);
  node.remove();
  const expand=text=>text.replace(/@@DISCO_ASSET_(\\d+)@@/g,(_,i)=>globalThis.__discoAsset(packed.paths[+i]));
  const style=document.createElement('style');style.textContent=expand(packed.css);document.head.append(style);
  const template=document.createElement('template');template.innerHTML=expand(packed.html);document.body.append(template.content);
  const source=document.getElementById('disco-code'), script=document.createElement('script');
  script.textContent=JSON.parse(source.textContent);source.remove();document.body.append(script);
  document.documentElement.dataset.standalone='ready';
})();`;
const title = entry.match(/<title>([^<]*)<\/title>/)?.[1] || '东北迪斯科';
const result = `<!doctype html>\n<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover"><meta name="theme-color" content="#171714"><title>${title}</title><style>html,body{margin:0;background:#171714}</style></head><body>\n<!-- Offline single-file snapshot; original artwork and CC0 audio notices are embedded. -->\n${embedded.join('\n')}\n<script type="application/json" id="disco-document">${safeJson({html,css,paths})}</script>\n<script type="application/json" id="disco-code">${safeJson(code)}</script>\n<script>${boot}</script>\n</body></html>\n`;
// Do not silently overwrite an older user deliverable; the caller can choose a new name.
await mkdir(dirname(output),{recursive:true});
await writeFile(output,result,{flag:'wx'});
const report={output,createdAt:new Date().toISOString(),bytes:(await stat(output)).size,sha256:digest(result),
  singleFile:true,assetCount:assets.length,assetBytes:assets.reduce((sum,a)=>sum+a.bytes,0),assets,
  sources:[...sourceHashes].map(([path,sha256])=>({path,sha256})),
  behavior:'Current UI showcase, original interactions, music and paper SFX; all media embedded as local Blob URLs.',
  validation:'Pending file:// offline browser verification'};
await writeFile(resolve(root,'docs/single-html-package.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({output,bytes:report.bytes,assetCount:report.assetCount,assetBytes:report.assetBytes,sha256:report.sha256},null,2));
