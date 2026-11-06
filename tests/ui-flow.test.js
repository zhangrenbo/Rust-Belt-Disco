import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {Window} from 'happy-dom';
import {SAVE_KEY,newGame} from '../src/station/state.js';

test('gameplay resumes through start, continue and slots while preserving manual saves and UI navigation',async()=>{
  const window=new Window({url:'http://localhost:5173/',settings:{disableJavaScriptFileLoading:true,disableCSSFileLoading:true,enableJavaScriptEvaluation:false}});
  const originals=new Map();
  const set=(name,value)=>{originals.set(name,Object.getOwnPropertyDescriptor(globalThis,name));Object.defineProperty(globalThis,name,{value,configurable:true,writable:true});};
  set('window',window);set('document',window.document);set('localStorage',window.localStorage);set('CustomEvent',window.CustomEvent);set('WheelEvent',window.WheelEvent);
  set('requestAnimationFrame',()=>1);set('cancelAnimationFrame',()=>{});
  window.matchMedia=()=>({matches:true,addEventListener(){},removeEventListener(){}});
  const q=selector=>window.document.querySelector(selector);
  const click=selector=>{assert.ok(q(selector),selector);q(selector).click();};
  const key=key=>window.document.dispatchEvent(new window.KeyboardEvent('keydown',{key,bubbles:true,cancelable:true}));
  const save=JSON.stringify(newGame());
  window.localStorage.setItem(SAVE_KEY+':auto',save);
  window.localStorage.setItem(SAVE_KEY+':01',save);
  window.localStorage.setItem(SAVE_KEY+':04','unreadable manual save');
  window.localStorage.setItem('unrelated-key','preserved');
  const snapshot=()=>Object.fromEntries(Array.from({length:window.localStorage.length},(_,i)=>{const k=window.localStorage.key(i);return [k,window.localStorage.getItem(k)];}));
  const before=snapshot();
  try{
    window.document.write(await readFile(new URL('../index.html',import.meta.url),'utf8'));
    assert.equal(q('#ui-only-notice').classList.contains('active'),false,'opening the application must not show the gameplay scene');
    assert.equal(q('#ui-only-title').textContent,'仅UI展示，无可游玩内容');
    assert.equal(q('#ui-only-notice button').textContent,'返回主菜单');
    await import('../scene.js?gameplay-resume-test='+Date.now());
    assert.equal(q('#ui-only-notice').classList.contains('active'),false,'initialization must leave the homepage visible');
    assert.equal(q('.view.active').dataset.view,'mainmenu');
    click('.mainmenu-choice-continue');assert.equal(q('.view.active').dataset.view,'station');
    assert.equal(q('[data-view=station]').dataset.storyNode,'arrival');
    assert.equal(q('#station-talk').open,false,'the night opening waits for the player to approach');
    key('Escape');assert.equal(q('#station-pause').open,true);
    click('#station-pause-menu');assert.equal(q('.view.active').dataset.view,'mainmenu');
    for(const choice of ['金钱为方','烈日灼心']){
      click('.mainmenu-choice-start');assert.equal(q('.view.active').dataset.view,'character-select');
      assert.equal(q('#ui-only-notice').classList.contains('active'),false,'Start opens character selection; this is still UI browsing');
      click('[data-character-choice="'+choice+'"]');assert.equal(q('.view.active').dataset.view,'character-select','unimplemented routes cannot silently start Dong');
      assert.match(q('#toast').textContent,/尚未开放/);
      key('Escape');assert.equal(q('.view.active').dataset.view,'mainmenu');
    }
    click('.mainmenu-choice-start');click('[data-character-choice="大厦崩塌"]');
    assert.equal(q('.view.active').dataset.view,'box','Dong now starts in the convenience-store box');
    assert.equal(q('#box-pieces').children.length,7);
    assert.equal(q('#box-parcel').disabled,true);
    click('[data-box-piece=gloves]');click('[data-box-move="0,-1"]');
    const boxProgress=window.localStorage.getItem('northeast-disco:box-opening:v1');
    key('Escape');assert.equal(q('.view.active').dataset.view,'mainmenu');
    click('.mainmenu-choice-continue');assert.equal(q('.view.active').dataset.view,'box');
    assert.equal(window.localStorage.getItem('northeast-disco:box-opening:v1'),boxProgress);
    key('Escape');click('.mainmenu-actions [data-open-view=entry]');
    assert.equal(window.document.querySelectorAll('[data-view=entry] [data-slot]').length,6);
    click('[data-view=entry] [data-slot="01"]');
    assert.equal(q('.station-slot-actions button:nth-child(2)').disabled,true,'the new box opening must not overwrite legacy station slots');
    click('.station-slot-actions button');assert.equal(q('#slot-dialog').open,false);assert.equal(q('.view.active').dataset.view,'station');
    click('#station-save-button');
    click('[data-view=entry] [data-slot="04"]');
    assert.ok(q('.station-slot-actions button').disabled);
    assert.ok(q('.station-slot-actions button:nth-child(2)').disabled);
    click('.station-slot-actions button:nth-child(3)');
    click('[data-view=entry] [data-slot="06"]');assert.ok(q('.station-slot-actions button').disabled);
    assert.equal(q('.station-slot-actions button:nth-child(2)').disabled,false);
    click('.station-slot-actions button:nth-child(2)');
    assert.equal(JSON.parse(window.localStorage.getItem(SAVE_KEY+':06')).node,'arrival');
    click('#station-archive-return');assert.equal(q('.view.active').dataset.view,'station');
    key('Escape');click('#station-pause-menu');
    click('.mainmenu-actions [data-open-view=settings]');
    click('[data-personnel-open]');assert.equal(q('.view.active').dataset.view,'explorer-dossier');
    const portraits = new Set();
    for (const tab of window.document.querySelectorAll('[data-dossier-person]')) {
      tab.click();
      assert.equal(q('[data-dossier-name]').textContent,tab.textContent);
      assert.equal(q('#dossier-record').getAttribute('aria-labelledby'),tab.id);
      assert.equal(window.document.querySelectorAll('.explorer-dossier-tabs [aria-selected=true]').length,1);
      const image = q('[data-dossier-face]').hidden ? q('[data-dossier-portrait]') : q('[data-dossier-face]');
      assert.match(image.alt,new RegExp(tab.textContent));portraits.add(image.src);
    }
    assert.equal(portraits.size,window.document.querySelectorAll('[data-dossier-person]').length,'each character selects a distinct real portrait');
    click('[data-dossier-person=li]');assert.equal(q('[data-dossier-role]').textContent,'镇上警官');
    click('[data-dossier-person=wang]');assert.equal(q('[data-dossier-place]').textContent,'家中／诊所');
    assert.doesNotMatch(q('.view-explorer-dossier').textContent,/生命|速度|暴击|攻击|防御|等级|经验|3000/,'detective records contain no combat or progression stats');
    assert.equal(q('[data-explorer-inc]'),null);
    click('[data-dossier-return]');assert.equal(q('.view.active').dataset.view,'settings');
    key('Escape');assert.equal(q('.view.active').dataset.view,'mainmenu');
    const after=snapshot();
    for(const k of [SAVE_KEY+':01',SAVE_KEY+':04','unrelated-key'])assert.equal(after[k],before[k],'start/continue/load do not overwrite unrelated manual saves');
    assert.equal(JSON.parse(after[SAVE_KEY+':auto']).node,'arrival');
    assert.equal(q('#ui-only-notice').classList.contains('active'),false);
  }finally{
    await window.happyDOM.abort();window.close();
    for(const[name,descriptor]of originals){if(descriptor)Object.defineProperty(globalThis,name,descriptor);else delete globalThis[name];}
  }
});

