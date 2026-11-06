"""Verify audio persistence using real, namespaced storage in a disposable Chrome CDP tab."""
import ast
import importlib.util
import json
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('volume_cdp', ROOT/'scripts/record-catalog-cdp-session.py')
cdp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cdp)
tree = ast.parse((ROOT/'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8-sig'))
audio_init = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
    and any(isinstance(t, ast.Name) and t.id == 'INIT' for t in n.targets))
prefix = 'qa-volume-autosave-' + str(uuid.uuid4()) + ':'
# Preserve actual persistence across reload while isolating every test key from player data.
storage_init = '''(() => {
  const native = window.localStorage, prefix = PREFIX;
  const keys = () => Array.from({length:native.length}, (_,i)=>native.key(i)).filter(k=>k.startsWith(prefix));
  const cleanup = () => keys().forEach(k=>native.removeItem(k));
  window.__volumeQaCleanup = cleanup;
  if (!native.getItem(prefix+'qa-unrelated')) {
    native.setItem(prefix+'northeast-disco.settings.v1', JSON.stringify({music:54}));
    native.setItem(prefix+'qa-unrelated', 'preserve');
  }
  Object.defineProperty(window, 'localStorage', {value:{
    getItem:k=>native.getItem(prefix+String(k)), setItem:(k,v)=>native.setItem(prefix+String(k),String(v)),
    removeItem:k=>native.removeItem(prefix+String(k)), clear:cleanup,
    key:i=>keys()[i]?.slice(prefix.length)??null, get length(){return keys().length;}
  }});
  window.__volumeQaErrors=[];
  addEventListener('error',e=>{if(e.error)window.__volumeQaErrors.push(String(e.error));});
'''.replace('PREFIX', json.dumps(prefix))
init = storage_init + audio_init[audio_init.index('  const Native = window.AudioContext;'):]
page = cdp.CatalogPage(init)
checks = []
measure = '''({values:Object.fromEntries([...document.querySelectorAll('input[data-audio-channel]')]
  .map(e=>[e.dataset.audioChannel,+e.value])), gain:window.__recordQA.master.gain.value,
  rms:window.__recordQA.rms(), saved:JSON.parse(localStorage.getItem('northeast-disco.settings.v1')),
  sourceCount:window.__recordQA.sources.length, view:document.querySelector('.view.active').dataset.view})'''

def ready(view='settings'):
    page.wait("document.querySelector('[data-view="+view+"].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def loaded():
    page.wait("document.querySelectorAll('.record-track').length===2&&document.querySelector('#app').dataset.musicState&&!document.querySelector('#app').classList.contains('is-view-switching')", timeout=20)

def open_settings():
    page.click('.mainmenu-actions [data-open-view=settings]')
    ready()

def audio(value, channel='music'):
    page.evaluate("(()=>{const e=document.querySelector('input[data-audio-channel="+channel+"]');e.value="+str(value)+";e.dispatchEvent(new Event('input',{bubbles:true}));})()")

def check(name, expression):
    page.wait(expression)
    checks.append({'check':name, **page.evaluate(measure)})
    print('PASS '+name, flush=True)

try:
    page.resize(1058,595)
    page.cdp.call('Page.navigate', {'url':'http://127.0.0.1:5173/'})
    loaded()
    open_settings()
    check('Initial actual music output uses stored volume', 'Math.abs(window.__recordQA.master.gain.value-.54)<.001')
    sources = page.evaluate('window.__recordQA.sources.length')
    page.click('button[data-quality=performance]')
    audio(11,'voice'); audio(23); audio(78,'sound')
    check('All three channels autosave without committing the graphics draft', "(()=>{const s=JSON.parse(localStorage.getItem('northeast-disco.settings.v1'));return s.voice===11&&s.music===23&&s.sound===78&&s.quality==='enhanced';})()")
    check('Music gain changes immediately without restarting the source', f'Math.abs(window.__recordQA.master.gain.value-.23)<.001&&window.__recordQA.sources.length==={sources}')
    page.click('[data-settings-return]'); ready('mainmenu'); open_settings()
    check('Return and reopen retain all sliders and actual sound', "['voice','music','sound'].every((c,i)=>+document.querySelector('input[data-audio-channel='+c+']').value===[11,23,78][i])&&Math.abs(window.__recordQA.master.gain.value-.23)<.001")
    page.click('[data-music-library-open]')
    page.wait("!document.querySelector('#settings-music-library').hidden")
    page.click('[data-record-next]')
    check('Changing records retains the automatically saved music volume', "document.querySelector('#app').dataset.musicTrack==='station'&&Math.abs(window.__recordQA.master.gain.value-.23)<.001")
    page.click('[data-record-play]'); page.click('[data-record-play]')
    check('Record pause and resume retain volume', "document.querySelector('#app').dataset.musicState==='playing'&&Math.abs(window.__recordQA.master.gain.value-.23)<.001")
    page.click('[data-music-library-close]')
    audio(0)
    page.key('Escape'); ready('mainmenu'); open_settings()
    check('Escape retains mute and silences the real signal', "document.querySelector('input[data-audio-channel=music]').value==='0'&&JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).music===0&&window.__recordQA.master.gain.value===0&&window.__recordQA.rms()<1e-7")
    page.cdp.call('Page.reload', {'ignoreCache':True}); loaded(); open_settings()
    check('Reload reads mute and other channels from actual namespaced localStorage', "['voice','music','sound'].every((c,i)=>+document.querySelector('input[data-audio-channel='+c+']').value===[11,0,78][i])&&window.__recordQA.master.gain.value===0")
    page.resize(844,475)
    r=page.rect('input[data-audio-channel=music]')
    page.click_at(r['x']+r['width']*.37, r['y']+r['height']/2)
    value=page.evaluate("+document.querySelector('input[data-audio-channel=music]').value")
    assert 30 <= value <= 44, value
    check('A real pointer drag in the small window saves its actual gain', f"JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).music==={value}&&Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001")
    page.cdp.call('Page.reload', {'ignoreCache':True}); loaded(); open_settings()
    check('Nonzero volume also survives reloading without Apply', f"+document.querySelector('input[data-audio-channel=music]').value==={value}&&Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001")
    page.click('[data-reset-settings]')
    check('Restore defaults remains a preview until Apply', f"document.querySelector('input[data-audio-channel=music]').value==='54'&&JSON.parse(localStorage.getItem('northeast-disco.settings.v1')).music==={value}")
    page.click('[data-settings-return]'); ready('mainmenu'); open_settings()
    check('Leaving the defaults preview restores the last autosaved volume', f"+document.querySelector('input[data-audio-channel=music]').value==={value}&&Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001")
    check('Test data remains scoped and there are no JavaScript errors', "localStorage.getItem('qa-unrelated')==='preserve'&&localStorage.length===2&&window.__volumeQaErrors.length===0")
    report={'transport':'fixed work Chrome, direct target-specific CDP 9223', 'checks':checks,
      'storage':'real browser localStorage under a unique test prefix; actual reloads; player keys untouched',
      'audio':'actual Web Audio gain and analyser before a muted test sink; physical speakers not tested',
      'errors':page.evaluate('window.__volumeQaErrors'), 'electronWindowVerified':False}
    (ROOT/'docs/volume-autosave-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'checks':len(checks),'errors':report['errors']}),flush=True)
finally:
    try:
        page.evaluate('window.__volumeQaCleanup?.()')
    finally:
        page.close()
