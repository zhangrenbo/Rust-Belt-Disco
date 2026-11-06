"""Check the character background edit in an isolated, silent Chrome CDP page."""
import argparse
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--before', action='store_true')
parser.add_argument('--prefix', default='character-newspaper')
parser.add_argument('--revision', default='v3')
args = parser.parse_args()
spec = importlib.util.spec_from_file_location('character_cdp', ROOT/'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
INIT = r'''(() => {
  const values=new Map();
  Object.defineProperty(window,'localStorage',{value:{getItem:k=>values.get(k)??null,
    setItem:(k,v)=>values.set(k,String(v)),removeItem:k=>values.delete(k),clear:()=>values.clear(),
    key:i=>[...values.keys()][i]??null,get length(){return values.size;}}});
  const connect=AudioNode.prototype.connect,gates=new WeakMap();
  AudioNode.prototype.connect=function(target,...args){
    if(target instanceof AudioDestinationNode){
      if(!gates.has(target)){const gate=new GainNode(this.context,{gain:0});connect.call(gate,target);gates.set(target,gate);}
      target=gates.get(target);
    }
    return connect.call(this,target,...args);
  };
  window.__characterErrors=[];
  addEventListener('error',e=>{if(e.error)__characterErrors.push(String(e.error));});
})();'''
page = cdp.CatalogPage(INIT)
screens = []

def key(name, vk):
    for kind in ('keyDown','keyUp'):
        page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':name,'code':name,'windowsVirtualKeyCode':vk})

def ready(view):
    page.wait(f"document.querySelector('[data-view={view}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def capture(name, width, height):
    page.resize(width,height)
    key('Home',36)
    # Keep edge-panning idle while capturing both camera positions.
    page.pointer(width/2,height/2)
    if name=='top':
        for _ in range(5): key('ArrowUp',38)
    page.wait("[...document.querySelectorAll('.character-art')].every(i=>i.complete&&i.naturalWidth>0)",timeout=20)
    path=ROOT/f'docs/{args.prefix}-{"before-" if args.before else args.revision+"-"}{name}-{width}.png'
    bounds=page.capture(path)
    metrics=page.evaluate("""(()=>({art:[...document.querySelectorAll('.character-art-base')].map(i=>({src:i.getAttribute('src'),size:[i.naturalWidth,i.naturalHeight],rect:{x:i.getBoundingClientRect().x,y:i.getBoundingClientRect().y,width:i.getBoundingClientRect().width,height:i.getBoundingClientRect().height},clip:getComputedStyle(i).clipPath,pointer:getComputedStyle(i).pointerEvents})),choices:document.querySelectorAll('[data-character-choice]').length,film:document.querySelector('#app').dataset.film}))()""")
    assert metrics['choices']==3 and all(i['pointer']=='none' for i in metrics['art']),metrics
    if not args.before and name=='default':
        metrics['badge']=page.evaluate("""(()=>{
          const r=document.querySelector('.character-police-badge-art').getBoundingClientRect();
          const li=document.querySelector('.character-art[src*="020_"]').getBoundingClientRect();
          const stage=document.querySelector('#app').getBoundingClientRect();
          const b={left:r.x+r.width*.273,right:r.x+r.width*.337,
            top:r.y+r.height*.694,bottom:r.y+r.height*.837};
          return {bounds:b,leftOfLiPhoto:b.right<li.left,lowerHalf:(b.top+b.bottom)/2>(li.top+li.bottom)/2,
            fullyVisible:b.left>=stage.left&&b.right<=stage.right&&b.top>=stage.top&&b.bottom<=stage.bottom};
        })()""")
        assert all(metrics['badge'][k] for k in ('leftOfLiPhoto','lowerHalf','fullyVisible')),metrics['badge']
    screens.append({'path':str(path),'viewport':[width,height],'gameBounds':bounds,'metrics':metrics})
    print('Captured '+path.name,flush=True)

try:
    page.resize(1440,810)
    page.cdp.call('Page.navigate',{'url':'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2",timeout=20)
    page.click('.mainmenu-actions [data-open-view=character-select]')
    ready('character-select')
    capture('default',1440,810)
    capture('top',1440,810)
    if not args.before:
        capture('default',844,475)
        capture('top',844,475)
        key('Home',36)
        page.focus('.character-choice-center')
        page.key('Enter')
        ready('ui-only')
        page.click('.ui-only-notice-button')
        ready('mainmenu')
        page.click('.mainmenu-actions [data-open-view=character-select]')
        ready('character-select')
        page.click('.character-return')
        ready('mainmenu')
        assert page.evaluate('localStorage.length')==0
    errors=page.evaluate('window.__characterErrors')
    assert not errors,errors
    report={'transport':'fixed work Chrome + direct CDP 9223; task-created background page',
      'screens':screens,'errors':errors,'realPlayerData':'untouched; isolated memory storage',
      'audio':'test output muted','selectionAndReturn':not args.before,
      'humanArtReview':'pending','electronWorkbenchInspected':False}
    (ROOT/f'docs/{args.prefix}-{"before-" if args.before else args.revision+"-"}verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
finally:
    page.close()
