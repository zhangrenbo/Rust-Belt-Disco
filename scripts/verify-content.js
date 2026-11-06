import {readFile,stat} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {musicTracks} from '../src/core/music.js';
import {uiSoundFiles} from '../src/core/ui-sounds.js';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const manifest=JSON.parse(await readFile(resolve(root,'docs/portable-content-manifest.json'),'utf8'));
for(const item of manifest.files){
  const actual=await readFile(resolve(root,item.destination));
  if(actual.length!==item.bytes||createHash('sha256').update(actual).digest('hex')!==item.sha256)throw new Error(`Migrated content differs: ${item.destination}`);
}
const html=await readFile(resolve(root,'index.html'),'utf8');
const css=await readFile(resolve(root,'app.css'),'utf8');
const paths=new Set([...html.matchAll(/(?:src|data-src|href)="([^"#]+)"/g)].map(m=>m[1].split('?')[0]));
for(const track of Object.values(musicTracks))paths.add(track.url);
for(const url of Object.values(uiSoundFiles))paths.add(url);
for(const m of css.matchAll(/url\(\s*(?:"([^"]*)"|'([^']*)'|([^)]*))\s*\)/g)){const url=m[1]??m[2]??m[3];if(!url.startsWith('data:'))paths.add(url);}
for(const relative of paths){if(relative.includes(':'))continue;const path=relative.replace(/^\//,'');try{await stat(resolve(root,path));}catch{await stat(resolve(root,'public',path));}}
console.log(`Verified ${manifest.files.length} migrated file hashes and ${paths.size} HTML/CSS resource paths.`);
