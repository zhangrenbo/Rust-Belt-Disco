"""Background Chrome CDP QA. Isolated tab storage; never touches real saves."""
import importlib.util,json,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
INIT="""const qaStore=JSON.parse(sessionStorage.getItem('boxQA')||'{}');Object.defineProperty(window,'localStorage',{value:{getItem:k=>qaStore[k]??null,setItem:(k,v)=>{qaStore[k]=String(v);sessionStorage.setItem('boxQA',JSON.stringify(qaStore));},removeItem:k=>{delete qaStore[k];sessionStorage.setItem('boxQA',JSON.stringify(qaStore));}}});window.qaErrors=[];window.addEventListener('error',e=>qaErrors.push(e.message));window.addEventListener('unhandledrejection',e=>qaErrors.push(String(e.reason)));HTMLMediaElement.prototype.play=()=>Promise.resolve();"""
page=cdp.CatalogPage(INIT)
try:
 page.resize(1440,900)
 page.cdp.call('Network.setBlockedURLs',{'urls':['http://127.0.0.1:5173/@vite/client']})
 page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/?box-qa=1'})
 page.wait('!!document.querySelector(".mainmenu-choice-continue")')
 page.evaluate('document.querySelector(".mainmenu-choice-start").click();document.querySelector(".character-choice-center").click()')
 page.wait('!!document.querySelector(".view-box.active")')
 page.wait('!document.querySelector("#app").classList.contains("is-view-switching")')
 time.sleep(.6)
 page.capture(ROOT/'docs/box-opening-start.png')
 report={'view':'box','sizes':[]}
 # Actual pointer drag, a legal upward move into the empty cell.
 r=page.rect('[data-box-piece=gloves]');x=r['x']+r['width']/2;y=r['y']+r['height']/2
 cell=page.rect('#box-board')['width']/5
 page.cdp.call('Input.dispatchMouseEvent',{'type':'mousePressed','x':x,'y':y,'button':'left','clickCount':1})
 page.cdp.call('Input.dispatchMouseEvent',{'type':'mouseMoved','x':x,'y':y-cell,'button':'left','buttons':1})
 page.cdp.call('Input.dispatchMouseEvent',{'type':'mouseReleased','x':x,'y':y-cell,'button':'left','clickCount':1})
 page.wait('JSON.parse(localStorage.getItem("northeast-disco:box-opening:v1")).moves>0')
 saved=page.evaluate('localStorage.getItem("northeast-disco:box-opening:v1")')
 page.evaluate('document.querySelector("#box-menu").click();document.querySelector(".mainmenu-choice-continue").click()')
 assert page.evaluate('localStorage.getItem("northeast-disco:box-opening:v1")')==saved
 page.cdp.call('Page.reload');page.wait('!!document.querySelector(".mainmenu-choice-continue")')
 page.evaluate('document.querySelector(".mainmenu-choice-continue").click()')
 assert page.evaluate('localStorage.getItem("northeast-disco:box-opening:v1")')==saved
 report['dragAndReload']=True
 for w,h in [(1440,900),(1200,900),(800,600),(844,390),(640,900),(1920,600)]:
  page.resize(w,h);time.sleep(.2)
  bounds=page.evaluate('''(()=>{const s=document.querySelector('#app').getBoundingClientRect();const elements=[...document.querySelectorAll('.view-box button,.box-rim,.box-caption,.box-footer')].filter(e=>e.getClientRects().length);return {stage:[s.width,s.height],outside:elements.filter(e=>{const r=e.getBoundingClientRect();return r.left<s.left-1||r.top<s.top-1||r.right>s.right+1||r.bottom>s.bottom+1}).map(e=>e.id||e.className),ratio:(()=>{const r=document.querySelector('#box-board').getBoundingClientRect();return r.width/r.height})(),scroll:document.documentElement.scrollHeight>innerHeight||document.documentElement.scrollWidth>innerWidth};})()''')
  assert not bounds['outside'] and not bounds['scroll'] and abs(bounds['ratio']-1.25)<.001,bounds
  report['sizes'].append({'viewport':[w,h],**bounds})
  if (w,h) in [(800,600),(844,390)]:page.capture(ROOT/f'docs/box-opening-{w}x{h}.png')
 page.resize(1440,900)
 # Solve from current state through legal UI moves, rather than editing the save.
 result=page.evaluate('''(async()=>{const {act,canRemove}=await import('/src/box/puzzle.js');const key='northeast-disco:box-opening:v1';let state=JSON.parse(localStorage.getItem(key)),actions=[];while(state.pieces.length){const queue=[{s:state,path:[]}],seen=new Set();let found;for(let i=0;i<queue.length&&i<100000;i++){const {s,path}=queue[i],p=s.pieces.find(canRemove);if(p){found={s:act(s,p.id,0,0,true),path:[...path,[p.id,0,0,true]]};break;}for(const p of s.pieces)for(const [dx,dy]of [[1,0],[-1,0],[0,1],[0,-1]]){const n=act(s,p.id,dx,dy);if(n===s)continue;const k=n.pieces.map(p=>p.id+':'+p.x+','+p.y).join('|');if(seen.has(k))continue;seen.add(k);queue.push({s:n,path:[...path,[p.id,dx,dy,false]]});}}if(!found)throw Error('No route');actions.push(...found.path);state=found.s;}for(const [id,dx,dy,remove]of actions){document.querySelector('[data-box-piece="'+id+'"]').click();const b=document.querySelector(remove?'#box-remove':'[data-box-move="'+dx+','+dy+'"]');if(b.disabled)throw Error('Blocked UI action');b.click();}return {actions:actions.length,remaining:document.querySelector('#box-pieces').children.length,parcelEnabled:!document.querySelector('#box-parcel').disabled};})()''')
 assert result['remaining']==0 and result['parcelEnabled'],result
 report['solution']=result
 page.capture(ROOT/'docs/box-opening-cleared.png')
 page.evaluate('document.querySelector("#box-parcel").click()')
 assert page.evaluate('!document.querySelector("#box-found").hidden')
 page.capture(ROOT/'docs/box-opening-found.png')
 page.evaluate('document.querySelector("#box-look-back").click();document.querySelector("#box-undo").click();document.querySelector("#box-undo").click()')
 assert page.evaluate('document.querySelector("#box-pieces").children.length===1&&document.querySelector("#box-parcel").disabled')
 page.evaluate('document.querySelector("#box-reset").click()')
 assert page.evaluate('document.querySelector("#box-pieces").children.length===7')
 report['undoResetPickup']=True
 report['errors']=page.evaluate('qaErrors');assert not report['errors'],report['errors']
 (ROOT/'docs/box-opening-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(report,ensure_ascii=False))
finally:page.close()
