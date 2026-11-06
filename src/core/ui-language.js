import {createUiTranslator} from './ui-language-copy.js';

export const UI_LANGUAGE_KEY = 'northeast-disco.ui-language.v1';

// Live presentation switching: no reload, story rewrite, or save migration.
export function installUiLanguage({window,document}) {
  const translate=createUiTranslator(), originals=new WeakMap();
  const html=document.documentElement, originalTitle=document.title;
  const excluded='script,style,[data-ui-language-switch],[data-view="station"],[data-view="box"],#station-talk,#station-notes';
  let stored;
  try { stored=window.localStorage.getItem(UI_LANGUAGE_KEY); } catch {}
  const requested=new window.URLSearchParams(window.location.search).get('lang');
  let language=(requested==='en'||requested==='zh')?requested:stored==='en'?'en':'zh';
  const attrs=['aria-label','alt','title','placeholder'];
  function valueFor(node,key,current) {
    let values=originals.get(node);
    if(!values) {values=new Map();originals.set(node,values);}
    let entry=values.get(key);
    if(!entry || current!==entry.rendered) entry={source:current};
    entry.rendered=language==='en'?translate(entry.source):entry.source;
    values.set(key,entry);
    return entry.rendered;
  }
  function localize(node) {
    const element=node.nodeType===window.Node.ELEMENT_NODE?node:node.parentElement;
    if(!element || element.closest(excluded)) return;
    if(node.nodeType===window.Node.TEXT_NODE) {
      const next=valueFor(node,'text',node.nodeValue);
      if(next!==node.nodeValue) node.nodeValue=next;
    } else if(node.nodeType===window.Node.ELEMENT_NODE) {
      for(const attr of attrs) if(node.hasAttribute(attr)) {
        const current=node.getAttribute(attr), next=valueFor(node,attr,current);
        if(next!==current) node.setAttribute(attr,next);
      }
      for(const child of node.childNodes) localize(child);
    }
  }
  const menuCells={start:'start',continue:'continue',archive:'saves',settings:'settings',exit:'exit'};
  for(const button of document.querySelectorAll('.mainmenu-choice')) {
    const img=button.querySelector('img');
    if(!img) continue;
    const label=document.createElement('span');
    label.className='english-menu-label';label.setAttribute('aria-hidden','true');
    const file=img.getAttribute('src').split('/').pop();
    label.dataset.sprite=Object.entries(menuCells).find(([key])=>file.includes(key))?.[1] || ({Start:'start',Continue:'continue',Saves:'saves',Settings:'settings',Exit:'exit'}[translate(img.alt)]);
    button.append(label);
  }
  const logo=document.createElement('div');
  logo.className='english-logo';logo.setAttribute('role','img');logo.setAttribute('aria-label','Rust Belt Disco');
  logo.innerHTML='<span class="english-logo-png" aria-hidden="true"></span>';
  document.querySelector('.mainmenu-logo')?.after(logo);
  for(const button of document.querySelectorAll('[data-character-choice]')) {
    const label=document.createElement('span');label.className='english-character-label';
    label.dataset.characterInk=button.classList.contains('character-choice-top')?'wang':button.classList.contains('character-choice-bottom')?'li':'dong';
    label.setAttribute('aria-hidden','true');document.querySelector('.character-world')?.append(label);
  }
  for(const img of document.querySelectorAll('.character-world img')) {
    const source=decodeURI(img.getAttribute('src')||img.dataset.src||'');
    if(/\/(008_|010_|025_|014_|015_|016_|023_|024_)/.test(source)) img.classList.add('english-replaced-ink');
  }
  const synopsis=document.createElement('div');synopsis.className='english-character-synopsis';
  synopsis.setAttribute('role','img');
  synopsis.setAttribute('aria-label','Dong Dianshi. His mother died in a fire. His father drank himself to death. The victims keep coming. The killer leaves no trace. Only two people saw the face: one cannot speak; the other is him, without his memory.');
  document.querySelector('.character-world')?.append(synopsis);
  const controls=[];
  for(const [selector,classes] of [['[data-view="mainmenu"]','ui-language-switch'],['.settings-paper-tabs','settings-paper-tab settings-library-tab settings-language-tab']]) {
    const parent=document.querySelector(selector);if(!parent) continue;
    const button=document.createElement('button');button.type='button';button.className=classes;
    button.dataset.uiLanguageSwitch='';button.addEventListener('click',()=>setLanguage(language==='zh'?'en':'zh'));
    parent.append(button);controls.push(button);
  }
  const observer=new window.MutationObserver(records=>{
    observer.disconnect();
    for(const record of records) {
      if(record.type==='childList') record.addedNodes.forEach(localize);else localize(record.target);
    }
    observe();
  });
  function observe(){observer.observe(document.body,{subtree:true,childList:true,characterData:true,attributes:true,attributeFilter:attrs});}
  function render(){
    observer.disconnect();html.lang=language==='en'?'en':'zh-CN';html.dataset.showcaseLanguage=language;
    document.title=language==='en'?'Rust Belt Disco':originalTitle;
    for(const button of controls) {
      button.textContent=language==='en'?'中文 / English UI':'中文 / English';
      button.setAttribute('aria-label',language==='en'?'Switch interface to Chinese; story remains Chinese':'切换为英文界面（剧情仍为中文）');
      if(button.classList.contains('ui-language-switch')) {
        const note=document.createElement('small');note.textContent=language==='en'?'Story: Chinese':'界面语言 · 剧情中文';button.append(note);
      }
    }
    localize(document.body);observe();
  }
  function setLanguage(next){
    if(!['zh','en'].includes(next)) return;
    language=next;try{window.localStorage.setItem(UI_LANGUAGE_KEY,next);}catch{}
    render();
  }
  for(const [group,name] of Object.entries({logo:'main-menu-logo-en.png',menu:'menu-en-atlas.png',character:'character-en-atlas.png',dossier:'dossier-en-atlas.png'})) {
    const image=new window.Image();image.src=new window.URL('assets/'+name,document.baseURI).href;
    image.decode().then(()=>html.classList.add('ui-ink-'+group)).catch(()=>{});
  }
  render();
  return {setLanguage,get language(){return language;},destroy(){observer.disconnect();}};
}
