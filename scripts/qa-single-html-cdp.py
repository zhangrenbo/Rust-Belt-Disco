"""Open the desktop HTML through file:// with networking disabled in a disposable Chrome target."""
import ast
import importlib.util
import json
import time
import uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
package=json.loads((ROOT/'docs/single-html-package.json').read_text(encoding='utf-8'))
artifact=Path(package['output'])
spec=importlib.util.spec_from_file_location('single_file_cdp',ROOT/'scripts/record-catalog-cdp-session.py')
cdp=importlib.util.module_from_spec(spec);spec.loader.exec_module(cdp)
tree=ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
audio_init=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign)
    and any(isinstance(t,ast.Name) and t.id=='INIT' for t in n.targets))
prefix='qa-single-html-'+str(uuid.uuid4())+':'
storage_init='''(() => {
  const native=window.localStorage,prefix=PREFIX;
  const keys=()=>Array.from({length:native.length},(_,i)=>native.key(i)).filter(k=>k.startsWith(prefix));
  window.__singleCleanup=()=>keys().forEach(k=>native.removeItem(k));
  const save=JSON.stringify({version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},inspected:[],
    position:{x:440,y:640},warningHeard:false,shadow:null,completed:false,updatedAt:'2026-09-13T00:00:00.000Z'});
  if(!native.getItem(prefix+'qa-unrelated')){
    for(const [key,value] of [['qa-unrelated','preserved'],['northeast-disco.settings.v1','{"music":54,"sound":84}'],
      ['northeast-disco:campaign:v1:auto',save],['northeast-disco:campaign:v1:01',save],['northeast-disco:campaign:v1:04','damaged save']])
      native.setItem(prefix+key,value);
  }
  window.__singleSave=save;
  Object.defineProperty(window,'localStorage',{value:{getItem:k=>native.getItem(prefix+String(k)),
    setItem:(k,v)=>native.setItem(prefix+String(k),String(v)),removeItem:k=>native.removeItem(prefix+String(k)),clear:window.__singleCleanup,
    key:i=>keys()[i]?.slice(prefix.length)??null,get length(){return keys().length}}});
  window.__singleErrors=[];window.__singleImageErrors=[];
  addEventListener('error',e=>{if(e.error)__singleErrors.push(String(e.error));
    else if(e.target?.tagName==='IMG')__singleImageErrors.push(e.target.getAttribute('src'));},true);
  addEventListener('unhandledrejection',e=>__singleErrors.push(String(e.reason)));
'''.replace('PREFIX',json.dumps(prefix))
page=cdp.CatalogPage(storage_init+audio_init[audio_init.index('  const Native'):])
checks=[];screens=[]

def check(name,expression):
    page.wait(expression,timeout=15)
    checks.append(name);print('PASS '+name,flush=True)

def ready(view):
    page.wait("document.querySelector('[data-view="+view+"].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def loaded():
    page.wait("document.documentElement?.dataset.standalone==='ready'&&document.querySelectorAll('.record-track').length===2&&window.__audioQA?.length===2",timeout=30)
    ready('mainmenu')

def capture(name):
    page.wait("[...document.querySelectorAll('.view.active img[src]:not([hidden])')].every(i=>i.complete&&i.naturalWidth>0)",timeout=15)
    path=ROOT/('docs/single-html-'+name+'.png')
    rect=page.capture(path);screens.append({'path':str(path.relative_to(ROOT)),**rect})

def audio(channel,value):
    page.evaluate("(()=>{const e=document.querySelector('input[data-audio-channel="+channel+"]');e.value="+str(value)+";e.dispatchEvent(new Event('input',{bubbles:true}));})()")

try:
    page.resize(1058,595)
    page.cdp.call('Network.emulateNetworkConditions',{'offline':True,'latency':0,'downloadThroughput':0,'uploadThroughput':0})
    page.cdp.call('Network.overrideNetworkState',{'offline':True,'latency':0,'downloadThroughput':0,'uploadThroughput':0})
    page.cdp.call('Network.setBlockedURLs',{'urls':['http://*','https://*']})
    page.cdp.call('Page.navigate',{'url':artifact.as_uri()})
    loaded()
    check('Local file boots without external resource requests', "location.protocol==='file:'&&document.querySelector('.view.active').dataset.view==='mainmenu'&&performance.getEntriesByType('resource').every(e=>e.name.startsWith('blob:')||e.name.startsWith('data:'))")
    # Chrome reports navigator.onLine=true for this file origin despite target-level blocking.
    # Prove the network block with an actual request instead of trusting that flag.
    blocked=page.evaluate('fetch("http://127.0.0.1:5173/__single_html_offline_probe__",{signal:AbortSignal.timeout(2500)}).then(()=>false,()=>true)')
    assert blocked;checks.append('An actual HTTP probe is blocked in the offline target')
    page.evaluate('performance.clearResourceTimings()')
    check('All embedded asset blocks were unpacked', 'document.querySelectorAll("script[data-file]").length===0')
    capture('mainmenu-1058')
    page.click('.mainmenu-actions [data-open-view=settings]');ready('settings')
    check('Theme music decodes and plays from embedded bytes', "__recordQA.sources.some(s=>s.node.buffer.duration>200)&&__recordQA.context.state==='running'")
    check('Paper sound decodes and plays on interaction', '__audioQA[1].sources.length>0&&__audioQA[1].sources[0].node.buffer.duration>.2')
    capture('settings-1058')
    audio('music',23);audio('sound',31)
    check('File version immediately saves separate volume channels', "(()=>{const s=JSON.parse(localStorage.getItem('northeast-disco.settings.v1'));return s.music===23&&s.sound===31;})()")
    page.click('[data-music-library-open]')
    check('Record library opens offline', '!document.querySelector("#settings-music-library").hidden')
    page.click('[data-record-next]')
    check('Second record decodes from embedded OGG', "document.querySelector('#app').dataset.musicTrack==='station'&&__recordQA.sources.some(s=>s.node.buffer.duration>50&&s.node.buffer.duration<60)")
    capture('library-1058')
    page.click('[data-record-play]')
    check('Record pause works', "document.querySelector('#app').dataset.musicState==='paused'")
    page.evaluate("(()=>{const e=document.querySelector('[data-record-seek]');e.value=20;e.dispatchEvent(new Event('input',{bubbles:true}));})()")
    page.click('[data-record-play]')
    check('Seek and resume use actual embedded audio', '__recordQA.sources.at(-1).offset>=19.9')
    page.click('[data-record-favorite]')
    check('Favorites persist in the file version', "JSON.parse(localStorage.getItem('northeast-disco.music-favorites.v1')).includes('station')")
    page.key('Escape');page.click('[data-personnel-open]');ready('explorer-dossier')
    for person in ['dong','li','wang']:
        page.click('[data-dossier-person='+person+']')
        check('Dynamic '+person+' portrait and handwriting load offline', "document.querySelector('.explorer-dossier-book').dataset.dossierCharacter==='"+person+"'&&document.querySelector('#dossier-record').classList.contains('has-handwritten-ink')&&document.querySelector('[data-dossier-portrait]').naturalWidth>0")
    capture('personnel-1058')
    page.key('Escape');ready('settings');page.key('Escape');ready('mainmenu')
    page.click('.mainmenu-choice-start');ready('character-select')
    check('All lazy character layers resolve to embedded images', "[...document.querySelectorAll('[data-view=character-select] img')].every(i=>i.complete&&i.naturalWidth>0&&i.src.startsWith('blob:'))")
    capture('characters-1058')
    page.focus('[data-character-choice="大厦崩塌"]');page.key('Enter');ready('ui-only')
    check('Current UI-only behavior remains intact', "document.querySelector('#ui-only-title').textContent==='仅UI展示，无可游玩内容'")
    page.key('Escape');ready('mainmenu')
    page.click('.mainmenu-actions [data-open-view=entry]');ready('entry')
    check('All six save tickets remain available', "document.querySelectorAll('[data-view=entry] [data-slot]').length===6")
    capture('saves-1058')
    page.click('[data-view=entry] [data-slot="01"]')
    check('Valid save details open', 'document.querySelector("#slot-dialog").open')
    page.click('#slot-dialog .station-slot-actions button:first-child');ready('ui-only')
    page.key('Escape');ready('mainmenu')
    page.click('.mainmenu-choice-continue');ready('ui-only');page.key('Escape');ready('mainmenu')
    check('Loading and continuing preserve original saves', "localStorage.getItem('northeast-disco:campaign:v1:01')===__singleSave&&localStorage.getItem('northeast-disco:campaign:v1:auto')===__singleSave&&localStorage.getItem('northeast-disco:campaign:v1:04')==='damaged save'")
    # Test every included binary, including CSS masks that have no DOM error event.
    count=page.evaluate('''(async()=>{let total=0;for(const item of ASSETS){const url=__discoAsset(item.path);
      const bytes=await (await fetch(url)).arrayBuffer();if(bytes.byteLength!==item.bytes)throw Error(item.path);total++;}return total;})()'''.replace('ASSETS',json.dumps([{'path':a['path'],'bytes':a['bytes']} for a in package['assets']])))
    assert count==package['assetCount'];checks.append('Every embedded asset is readable at its original byte size')
    check('No external file or HTTP resource requests', "performance.getEntriesByType('resource').every(e=>e.name.startsWith('blob:')||e.name.startsWith('data:'))")
    check('No script or image loading errors', '__singleErrors.length===0&&__singleImageErrors.length===0')
    page.cdp.call('Page.reload',{'ignoreCache':True});loaded()
    page.resize(844,475)
    page.click('.mainmenu-actions [data-open-view=settings]');ready('settings')
    check('Offline reload restores saved music and sound volumes', "document.querySelector('[data-audio-channel=music]').value==='23'&&document.querySelector('[data-audio-channel=sound]').value==='31'&&Math.abs(__recordQA.master.gain.value-.23)<.001&&Math.abs(__audioQA[1].master.gain.value-.31)<.001")
    page.click('[data-music-library-open]')
    check('Favorite survives reopening the local HTML', "JSON.parse(localStorage.getItem('northeast-disco.music-favorites.v1')).includes('station')")
    capture('library-844')
    check('Narrow window still has no errors or external requests', "__singleErrors.length===0&&__singleImageErrors.length===0&&performance.getEntriesByType('resource').every(e=>e.name.startsWith('blob:')||e.name.startsWith('data:'))")
    report={'artifact':str(artifact),'sha256':package['sha256'],'browser':'fixed work Chrome + direct CDP 9223',
      'offline':True,'checks':checks,'screens':screens,'assetsReadable':count,
      'audio':'Actual decoded Web Audio, isolated analysers with muted output; physical speakers not tested.',
      'storage':'Real file-origin localStorage under a unique test prefix; player data unchanged; cleanup after test.',
      'errors':page.evaluate('__singleErrors'),'imageErrors':page.evaluate('__singleImageErrors')}
    (ROOT/'docs/single-html-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'checks':len(checks),'assets':count,'screens':len(screens)}),flush=True)
except Exception:
    try: print('DIAGNOSTIC',page.evaluate("({ready:document.readyState,state:document.documentElement.dataset.standalone,errors:window.__singleErrors,images:window.__singleImageErrors,view:document.querySelector('.view.active')?.dataset.view})"),flush=True)
    except Exception: pass
    raise
finally:
    try: page.evaluate('window.__singleCleanup?.()')
    finally: page.close()
