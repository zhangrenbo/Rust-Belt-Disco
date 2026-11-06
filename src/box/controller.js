import {BOX_SAVE_KEY,objects,freshPuzzle,validPuzzle,canMove,canRemove,act} from './puzzle.js';

// Local SVG drawings keep the prototype objects editable and require no new downloads.
const drawings={
 tin:'<rect x="14" y="8" width="172" height="120" rx="16" fill="#53685b" stroke="#25382f" stroke-width="7"/><rect x="24" y="18" width="152" height="100" rx="12" fill="none" stroke="#97a18a" stroke-width="2"/><path d="M46 39h108v60H46z" fill="#aa9874"/><path d="M67 57h66M67 70h66M85 83h30" stroke="#746c54" stroke-width="3"/>',
 book:'<path d="M46 4h105l8 125H42z" fill="#8c5842" stroke="#382d25" stroke-width="5"/><path d="M55 7v118" stroke="#c8a778" stroke-width="5"/><path d="M79 28h52v54H79z" fill="#c7b997"/><text x="105" y="62" text-anchor="middle" font-size="18" fill="#524b37">账</text>',
 case:'<rect x="8" y="36" width="184" height="65" rx="24" fill="#49392c" stroke="#9c8560" stroke-width="4"/><path d="M13 73h174" stroke="#201e19" stroke-width="4"/><path d="M86 66h28v15H86z" fill="#ad986c"/>',
 cloth:'<path d="M15 21l172 8-9 85-163-7z" fill="#837868" stroke="#4c493c" stroke-width="4"/><path d="M34 25v80m28-79v82m28-81v83m28-82v84m28-82v83M17 50l166 7M17 78l163 8" stroke="#b1a48a" stroke-width="5"/><path d="M19 109v15m10-15v15m11-14v15m11-15v15m11-14v15" stroke="#968b72" stroke-width="3"/>',
 torch:'<path d="M77 13h46l-8 25v82H85V38z" fill="#565c50" stroke="#262f2a" stroke-width="5"/><path d="M75 9h50v15H75z" fill="#a6a68c"/><rect x="91" y="47" width="18" height="21" rx="3" fill="#232f29"/><path d="M88 80h24m-24 8h24m-24 8h24" stroke="#91957c" stroke-width="3"/>',
 gloves:'<path d="M32 108l-10-44 4-37q8-9 13 0l3 24 5-34q8-10 13 1l1 33 8-30q9-7 12 4l-3 35 10-16q12-5 12 8l-17 56z" fill="#988c6e" stroke="#5c5947" stroke-width="4"/><path d="M115 111l-12-43 7-37q8-9 13 0l2 24 8-34q8-10 13 1l-1 33 10-30q9-7 12 4l-5 35 10-16q12-5 12 8l-20 56z" fill="#7f7a63" stroke="#514f40" stroke-width="4"/>',
 tape:'<rect x="18" y="22" width="164" height="92" rx="6" fill="#343c35" stroke="#191f1b" stroke-width="5"/><path d="M30 31h140v48H30z" fill="#baa982"/><circle cx="64" cy="57" r="16" fill="#465044"/><circle cx="136" cy="57" r="16" fill="#465044"/><path d="M67 91h66l9 19H58z" fill="#687061"/>',
};
export function installBoxOpening(ui){
 const $=id=>document.getElementById(id),view=document.querySelector('[data-view="box"]');
 let state=null,selected=null,history=[],gesture=null;
 let storage;try{storage=window.localStorage;}catch{}
 function read(){try{const s=JSON.parse(storage?.getItem(BOX_SAVE_KEY)||'null');return validPuzzle(s)?s:null;}catch{return null;}}
 function persist(){try{storage.setItem(BOX_SAVE_KEY,JSON.stringify(state));$('box-save-status').textContent='进度自动保存';}catch{$('box-save-status').textContent='保存失败，请保持页面打开';}window.dispatchEvent(new window.Event('disco:box-save'));}
 function status(text){$('box-status').textContent=text;}
 function render(){
  const focusId=document.activeElement?.dataset.boxPiece;
  $('box-pieces').replaceChildren(...state.pieces.map(p=>{
   const b=document.createElement('button');b.type='button';b.className='box-piece'+(p.id===selected?' is-selected':'');b.dataset.boxPiece=p.id;b.setAttribute('aria-label',p.name+'，选中后用方向键移动');b.setAttribute('aria-pressed',String(p.id===selected));
   b.style.cssText=`left:${p.x*20}%;top:${p.y*25}%;width:${p.w*20}%;height:${p.h*25}%;`;
   b.innerHTML=`<svg viewBox="0 0 200 136" aria-hidden="true">${drawings[p.id]}</svg><span>${p.name}</span>`;
   b.addEventListener('click',()=>select(p.id));
   b.addEventListener('pointerdown',e=>{if(e.button!==0)return;selected=p.id;gesture={id:p.id,x:e.clientX,y:e.clientY};b.setPointerCapture(e.pointerId);b.classList.add('is-selected');});
   b.addEventListener('pointerup',e=>{if(!gesture)return;const g=gesture;gesture=null;const r=$('box-board').getBoundingClientRect(),dx=e.clientX-g.x,dy=e.clientY-g.y;if(Math.hypot(dx,dy)<r.width*.035){select(g.id);return;}const horizontal=Math.abs(dx)>Math.abs(dy),axis=horizontal?Math.sign(dx):Math.sign(dy);let steps=Math.max(1,Math.round(Math.abs(horizontal?dx:dy)/(r.width/5)));let next=state;while(steps-->0){const n=act(next,g.id,horizontal?axis:0,horizontal?0:axis);if(n===next)break;next=n;}if(next!==state)commit(next);else status('这边被挡住了，先挪开旁边的旧物。');});
   b.addEventListener('pointercancel',()=>gesture=null);return b;
  }));
  $('box-removed').replaceChildren(...objects.filter(o=>!state.pieces.some(p=>p.id===o.id)).map(o=>{const s=document.createElement('span');s.textContent=o.name;return s;}));
  $('box-count').textContent=`已取出 ${objects.length-state.pieces.length} / ${objects.length} 件旧物`;
  $('box-undo').disabled=!history.length;$('box-parcel').disabled=state.pieces.length>0||state.taken;
  $('box-parcel').setAttribute('aria-label',state.pieces.length?'箱底的纸包，先移开上面的旧物':'拿起箱底的纸包');
  $('box-board').classList.toggle('is-clear',!state.pieces.length);
  $('box-found').hidden=!state.taken;
  controls();
  if(focusId)view.querySelector(`[data-box-piece="${focusId}"]`)?.focus({preventScroll:true});
 }
 function controls(){const p=state.pieces.find(p=>p.id===selected);$('box-remove').disabled=!canRemove(p);view.querySelectorAll('[data-box-move]').forEach(b=>{const [dx,dy]=b.dataset.boxMove.split(',').map(Number);b.disabled=!canMove(state.pieces,selected,dx,dy);});}
 function select(id){selected=id;view.querySelectorAll('[data-box-piece]').forEach(b=>{b.classList.toggle('is-selected',b.dataset.boxPiece===id);b.setAttribute('aria-pressed',String(b.dataset.boxPiece===id));});const p=state.pieces.find(p=>p.id===id);status(canRemove(p)?`${p.name}已经挪到缺口，可以取出了。`:`选中了${p.name}。试着挪向空出来的位置。`);controls();}
 function commit(next){if(next===state)return;history.push(structuredClone(state));state=next;persist();render();if(!state.pieces.length){status('旧物都移开了。拿起箱底的纸包。');$('box-parcel').focus({preventScroll:true});}else if(selected&&state.pieces.some(p=>p.id===selected))select(selected);else status('放到一边了。再看看剩下的旧物。');}
 function move(dx,dy){if(!state||state.taken)return;const next=act(state,selected,dx,dy);if(next===state)status('这边被挡住了，先挪开旁边的旧物。');else commit(next);}
 function start(resume=false){state=resume?(read()||freshPuzzle()):freshPuzzle();selected=null;history=[];persist();ui.setActiveView('box');render();status(state.pieces.length?'纸包压在最下面。先给这些旧物腾个位置。':'箱底的纸包露出来了。');(state.taken?$('box-look-back'):$('box-pieces').querySelector('button')||$('box-parcel')).focus({preventScroll:true});}
 $('box-remove').addEventListener('click',()=>{commit(act(state,selected,0,0,true));if(state.pieces.length)$('box-pieces').querySelector('button')?.focus({preventScroll:true});});
 $('box-undo').addEventListener('click',()=>{if(!history.length)return;state=history.pop();selected=null;persist();render();status('已撤回上一步。');});
 $('box-reset').addEventListener('click',()=>{history.push(structuredClone(state));state=freshPuzzle();selected=null;persist();render();status('旧物回到了原来的位置。可以撤回这次整理。');});
 $('box-parcel').addEventListener('click',()=>{if(state.pieces.length||state.taken)return;commit({...state,taken:true});$('box-look-back').focus({preventScroll:true});});
 $('box-look-back').addEventListener('click',()=>{state={...state,taken:false};persist();render();$('box-parcel').focus({preventScroll:true});});
 for(const id of ['box-menu','box-found-menu'])$(id).addEventListener('click',()=>ui.setActiveView('mainmenu'));
 view.querySelectorAll('[data-box-move]').forEach(b=>b.addEventListener('click',()=>move(...b.dataset.boxMove.split(',').map(Number))));
 document.addEventListener('keydown',e=>{if(ui.getActiveView()!=='box'||!state)return;if(e.key==='Escape'){e.preventDefault();ui.setActiveView('mainmenu');return;}if(state.taken)return;const d={ArrowLeft:[-1,0],ArrowRight:[1,0],ArrowUp:[0,-1],ArrowDown:[0,1]}[e.key];if(d){e.preventDefault();move(...d);}});
 if(ui.getActiveView()==='box')ui.setActiveView('mainmenu');
 return {start,hasSave:()=>Boolean(read())};
}
