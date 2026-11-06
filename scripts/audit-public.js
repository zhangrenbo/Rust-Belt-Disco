import {readFile,readdir,stat,writeFile} from 'node:fs/promises';
import {resolve,relative,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const sources=['index.html','app.css','src/station/station.css','src/station/controller.js'];
const keep=new Set();
for(const name of sources){
 const source=await readFile(resolve(root,name),'utf8');
 for(const m of source.matchAll(/["']\/?((?:assets|media)\/[^"'<>]+\.(?:png|mp4|webp|svg))["']/g))keep.add(m[1]);
 for(const m of source.matchAll(/["']((?:settings-|exit-dialog-)[^"']+\.png)["']/g))keep.add(m[1]);
}
const imported=JSON.parse(await readFile(resolve(root,'docs/portable-content-manifest.json'),'utf8'));
for(const file of imported.files)if(file.destination.startsWith('public/'))keep.add(file.destination.slice(7));
const files=[];
async function scan(path){for(const e of await readdir(path,{withFileTypes:true})){const full=resolve(path,e.name);if(e.isDirectory())await scan(full);else{const name=relative(resolve(root,'public'),full).replaceAll('\\','/');files.push({path:name,bytes:(await stat(full)).size,used:keep.has(name)});}}}
await scan(resolve(root,'public'));
const report={keep:files.filter(f=>f.used),unused:files.filter(f=>!f.used)};
await writeFile(resolve(root,'docs/runtime-assets-audit.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify({runtimeFiles:report.keep.length,runtimeBytes:report.keep.reduce((n,f)=>n+f.bytes,0),unusedFiles:report.unused.length,unusedBytes:report.unused.reduce((n,f)=>n+f.bytes,0)}));
