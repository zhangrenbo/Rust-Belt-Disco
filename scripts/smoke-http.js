import {readFile} from 'node:fs/promises';
import assert from 'node:assert/strict';
const base=process.argv[2]??'http://127.0.0.1:4173/';
const response=await fetch(base);assert.equal(response.status,200);
const html=await response.text();assert.match(html,/station-world/);assert.match(html,/event-dossier-handwriting/);
const refs=new Set([...html.matchAll(/(?:src|data-src|href)="([^"#]+)"/g)].map(m=>new URL(m[1],base).href));
const controller=await readFile(new URL('../src/station/controller.js',import.meta.url),'utf8');
for(const m of controller.matchAll(/['"](media\/[^'"]+\.png)['"]/g))refs.add(new URL(m[1],base).href);
const cssLinks=[...refs].filter(url=>url.endsWith('.css'));
for(const link of cssLinks){
 const cssResponse=await fetch(link);assert.equal(cssResponse.status,200);const css=await cssResponse.text();
 for(const m of css.matchAll(/url\(\s*(?:"([^"]*)"|'([^']*)'|([^)]*))\s*\)/g)){const url=m[1]??m[2]??m[3];if(!url.startsWith('data:'))refs.add(new URL(url,link).href);}
}
for(const url of refs){const result=await fetch(url,{method:'HEAD'});assert.equal(result.status,200,url);if(/\.(png|svg)$/.test(url))assert.match(result.headers.get('content-type')??'',/^image\//,url);}
console.log(`Built HTML plus ${refs.size} asset/script/style URLs respond correctly; image routes checked with HEAD only.`);
