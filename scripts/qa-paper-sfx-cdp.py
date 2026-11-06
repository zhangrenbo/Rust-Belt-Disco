"""Real UI clicks and Web Audio, with silent sinks and namespaced persistent test storage."""
import ast
import importlib.util
import json
import time
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('paper_cdp', ROOT / 'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec); spec.loader.exec_module(cdp)
tree = ast.parse((ROOT / 'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
audio_init = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
    and any(isinstance(t, ast.Name) and t.id == 'INIT' for t in n.targets))
prefix = 'qa-paper-sfx-' + str(uuid.uuid4()) + ':'
storage_init = '''(() => {
  const native=window.localStorage, prefix=PREFIX;
  const keys=()=>Array.from({length:native.length},(_,i)=>native.key(i)).filter(k=>k.startsWith(prefix));
  const cleanup=()=>keys().forEach(k=>native.removeItem(k)); window.__paperCleanup=cleanup;
  if(!native.getItem(prefix+'qa-unrelated')){
    native.setItem(prefix+'qa-unrelated','preserved');
    native.setItem(prefix+'northeast-disco.settings.v1',JSON.stringify({music:54,sound:84}));
  }
  Object.defineProperty(window,'localStorage',{value:{
    getItem:k=>native.getItem(prefix+String(k)),setItem:(k,v)=>native.setItem(prefix+String(k),String(v)),
    removeItem:k=>native.removeItem(prefix+String(k)),clear:cleanup,
    key:i=>keys()[i]?.slice(prefix.length)??null,get length(){return keys().length}
  }});
  window.__paperErrors=[];
  addEventListener('error',e=>{if(e.error)__paperErrors.push(String(e.error));});
  addEventListener('unhandledrejection',e=>__paperErrors.push(String(e.reason)));
'''.replace('PREFIX', json.dumps(prefix))
page = cdp.CatalogPage(storage_init + audio_init[audio_init.index('  const Native'):])
checks = []

def loaded():
    page.wait("document.querySelectorAll('.record-track').length===2&&window.__audioQA?.length===2&&!document.querySelector('#app').classList.contains('is-view-switching')", timeout=20)
    page.wait("performance.getEntriesByType('resource').filter(x=>x.name.includes('/media/sfx/')).length===4")
    time.sleep(.3)

def ready(view):
    page.wait("document.querySelector('[data-view="+view+"].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def assert_js(name, expression):
    page.wait(expression)
    checks.append({'check': name, 'passed': True})
    print('PASS ' + name, flush=True)

def effect(name, action, count=1):
    page.evaluate('''(()=>{const qa=__audioQA[1];window.__paperStart=qa.sources.length;window.__paperPeak=0;
      window.__paperSampler=setInterval(()=>{__paperPeak=Math.max(__paperPeak,qa.rms());},8);})()''')
    action(); time.sleep(.85)
    result = page.evaluate('''(()=>{clearInterval(__paperSampler);const qa=__audioQA[1];return {
      count:qa.sources.length-__paperStart, peakRms:__paperPeak,gain:qa.master.gain.value,
      durations:qa.sources.slice(__paperStart).map(s=>s.node.buffer.duration)}})()''')
    assert result['count'] == count, (name, result)
    if count: assert result['peakRms'] > .0001, (name, result)
    checks.append({'check': name, **result})
    print('PASS ' + name + ' ' + json.dumps(result), flush=True)

def audio(value, channel='sound', change=False):
    page.evaluate("(()=>{const input=document.querySelector('input[data-audio-channel="+channel+"]');input.value="+str(value)+";input.dispatchEvent(new Event('input',{bubbles:true}));"+
        ("input.dispatchEvent(new Event('change',{bubbles:true}));" if change else '')+"})()")

try:
    page.resize(1058,595)
    page.cdp.call('Page.navigate', {'url':'http://127.0.0.1:5173/'})
    loaded()
    assert_js('No paper sound during initial page load', '__audioQA[1].sources.length===0')
    effect('Opening settings turns paper once', lambda: page.click('.mainmenu-actions [data-open-view=settings]'))
    ready('settings')
    effect('Hover does not rustle', lambda: page.pointer(450,200), 0)
    effect('Selecting the already selected quality is silent', lambda: page.click('button[data-quality=enhanced]'), 0)
    effect('A setting stamp makes one soft contact', lambda: page.click('[data-game-toggle=name]'))
    audio(0, 'music')
    assert_js('Music mute does not mute the independent sound channel', '__audioQA[0].master.gain.value===0&&__audioQA[1].master.gain.value>.8')
    effect('Opening record library turns paper with music muted', lambda: page.click('[data-music-library-open]'))
    effect('Changing the record on its paper list makes a short rustle', lambda: page.click('[data-record-next]'))
    effect('Escape from record library turns paper once', lambda: page.key('Escape'))
    effect('Opening personnel records turns paper', lambda: page.click('[data-personnel-open]'))
    ready('explorer-dossier')
    effect('Selecting another character paper tab rustles', lambda: page.click('[data-dossier-person=li]'))
    def arrow():
        page.focus('[data-dossier-person=li]')
        for kind in ['keyDown','keyUp']:
            page.cdp.call('Input.dispatchKeyEvent',{'type':kind,'key':'ArrowRight','code':'ArrowRight','windowsVirtualKeyCode':39})
    effect('Keyboard arrow switches the paper tab with the same sound', arrow)
    effect('Reselecting the same character is silent', lambda: page.click('[data-dossier-person=wang]'), 0)
    effect('Disabled fourth character is silent', lambda: page.click('#dossier-tab-fourth'), 0)
    effect('Returning from the book turns paper', lambda: page.click('[data-dossier-return]'))
    ready('settings')
    effect('Sound zero suppresses the slider audition', lambda: audio(0, change=True), 0)
    effect('Muted sound remains silent inside the library', lambda: page.click('[data-music-library-open]'), 0)
    effect('Muted return does not queue a future sound', lambda: page.key('Escape'), 0)
    audio(35, 'music')
    assert_js('Music volume changes leave sound at zero', 'Math.abs(__audioQA[0].master.gain.value-.35)<.001&&__audioQA[1].master.gain.value===0')
    page.resize(844,475)
    def drag():
        r=page.rect('input[data-audio-channel=sound]')
        page.click_at(r['x']+r['width']*.27,r['y']+r['height']/2)
    effect('Actual small-window sound slider auditions once on release', drag)
    saved=page.evaluate("JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).sound")
    assert 20 <= saved <= 34, saved
    effect('Return retains the new volume without Apply', lambda: page.click('[data-settings-return]'))
    ready('mainmenu')
    effect('Opening save archive turns paper', lambda: page.click('.mainmenu-actions [data-open-view=entry]'))
    ready('entry')
    effect('Opening a save ticket makes a short paper sound', lambda: page.click('[data-view=entry] [data-slot="01"]'))
    effect('Escape closes the save ticket with a short paper sound', lambda: page.key('Escape'))
    page.cdp.call('Page.reload', {'ignoreCache': True}); loaded()
    assert_js('Reload retains sound gain and stays silent until input', f'__audioQA[1].sources.length===0&&Math.abs(__audioQA[1].master.gain.value-{saved}/100)<.001')
    effect('Reloaded UI plays at the stored sound level', lambda: page.click('.mainmenu-actions [data-open-view=settings]'))
    ready('settings')
    effect('Apply settings stamps once', lambda: page.click('[data-save-settings]'))
    assert_js('Player keys remain isolated and no JavaScript errors occurred', "localStorage.getItem('qa-unrelated')==='preserved'&&__paperErrors.length===0&&localStorage.length===2")
    report={'transport':'persistent work Chrome, direct target-specific CDP 9223', 'checks':checks,
        'audio':'Real decoded files, Web Audio sources, independent gain and analyser before silent output; physical speakers not auditioned.',
        'storage':'Unique real localStorage test prefix across reloads, cleaned after testing; player keys untouched.',
        'humanListeningApproved':False,'electronWindowVerified':False, 'errors':page.evaluate('__paperErrors')}
    (ROOT/'docs/paper-sfx-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'checks':len(checks),'savedSound':saved}),flush=True)
finally:
    try: page.evaluate('window.__paperCleanup?.()')
    finally: page.close()
