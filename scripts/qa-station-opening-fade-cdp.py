"""Opening fade from black for the A000 station entry, in an isolated silent Chrome CDP page.

A page-side setInterval sampler (rAF is throttled in occluded windows) records every
fade state during the natural entry, so the transient full-black window is observed
deterministically instead of polled for. Screenshots of the black/mid/done states use
a manual re-arm of the same classes.
"""
import importlib.util
import json
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('fade_cdp', ROOT/'scripts/record-catalog-cdp-session.py')
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
  window.__fadeErrors=[];
  addEventListener('error',e=>{if(e.error)__fadeErrors.push(String(e.error));});
})();'''
page = cdp.CatalogPage(INIT)
page.cdp.call('Network.setCacheDisabled', {'cacheDisabled': True})
checks, screens = [], []

def check(name, exp, timeout=15):
    page.wait('Boolean('+exp+')', timeout=timeout)
    checks.append(name)
    print('PASS '+name, flush=True)

def pass_py(name):
    checks.append(name)
    print('PASS '+name, flush=True)

def ready(view):
    page.wait(f"document.querySelector('[data-view={view}].active')&&!document.querySelector('#app').classList.contains('is-view-switching')")

def opacity():
    return float(page.evaluate("getComputedStyle(document.querySelector('.station-opening-fade')).opacity"))

def capture(name, w, h):
    page.resize(w, h)
    path = ROOT/f'docs/station-opening-fade-{name}-{w}.png'
    screens.append({'path': str(path), 'viewport': [w, h], 'gameBounds': page.capture(path)})
    print('Captured '+path.name, flush=True)

def assert_samples(record):
    assert record, 'no samples recorded'
    assert record[0]['o'] == 1, 'first sample was not fully black'
    first_dim = next((i for i, s in enumerate(record) if s['o'] < 1), None)
    assert first_dim is not None, 'opacity never left 1'
    black_hold = first_dim / len(record) and first_dim
    assert black_hold >= 10, f'black hold too short: {black_hold} samples'
    ops = [s['o'] for s in record[first_dim:]]
    assert ops[-1] == 0, 'fade did not reach full transparency'
    assert all(ops[i] >= ops[i+1] - 1e-3 for i in range(len(ops)-1)), 'opacity was not monotonically decreasing'
    fade_duration = (next(s['t'] for s in record if s['o'] == 0) - record[first_dim]['t']) / 1000
    assert 1.5 <= fade_duration <= 3.8, f'fade took {fade_duration:.2f}s'
    return {'entryT': record[0]['t'], 'blackSamples': black_hold, 'fadeStartT': record[first_dim]['t'],
            'fadeDurationS': round(fade_duration, 2), 'samples': len(record)}

try:
    page.resize(1440, 810)
    page.cdp.call('Page.navigate', {'url': 'http://127.0.0.1:5173/'})
    page.wait("document.querySelector('#app')?.dataset.musicState&&document.querySelectorAll('.record-track').length===2", timeout=20)
    ready('mainmenu')
    check('Launch stays on the homepage', "document.querySelector('.view-mainmenu.active')")
    page.click('.mainmenu-choice-start')
    ready('character-select')
    page.evaluate("""(()=>{
      window.__fadeSamples=[];
      setInterval(()=>{
        const f=document.querySelector('.station-opening-fade');
        if(f&&document.querySelector('[data-view=station].active')){
          window.__fadeSamples.push({t:performance.now(),o:Number(getComputedStyle(f).opacity),r:f.classList.contains('is-revealing')});
        }
      },16);
    })()""")
    page.focus('[data-character-choice="大厦崩塌"]')
    page.key('Enter')
    ready('station')
    check('Confirming Dong enters the A000 night station', "document.querySelector('[data-view=station]').dataset.storyNode==='arrival'")
    check('The scene is fully black at the moment of entry', "getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='1'")
    check('The fade layer covers every scene layer and never intercepts input', "(()=>{const s=getComputedStyle(document.querySelector('.station-opening-fade'));return s.position==='absolute'&&s.zIndex==='1200'&&s.pointerEvents==='none'&&s.transitionDuration==='2.2s'&&s.transitionDelay==='0.5s';})()")
    check('Reveal begins once the night backdrop has loaded', "document.querySelector('.station-opening-fade').classList.contains('is-revealing')", timeout=20)
    check('The running transition is exactly 2.2s after a 0.5s hold', "(()=>{const a=document.querySelector('.station-opening-fade').getAnimations().filter(x=>x.transitionProperty==='opacity');const t=a.length&&a[0].effect.getTiming();return a.length===1&&t.delay===500&&t.duration===2200;})()", timeout=5)
    check('The slow fade completes into a fully visible scene', "getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='0'", timeout=10)
    timing = assert_samples(page.evaluate('window.__fadeSamples'))
    pass_py('Entry holds full black, then fades in monotonically over ~2.2s')
    # Deterministic visual states: re-arm the same classes, then fade again.
    page.evaluate("(()=>{const f=document.querySelector('.station-opening-fade');f.classList.remove('is-revealing');void f.offsetWidth;})()")
    check('Re-armed overlay is fully black with no transition', "getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='1'&&getComputedStyle(document.querySelector('.station-opening-fade')).transitionDuration==='0s'")
    capture('black', 1440, 810)
    page.evaluate("document.querySelector('.station-opening-fade').classList.add('is-revealing')")
    page.click('#station-grandmother-marker')
    clickOpacity = opacity()
    assert clickOpacity > .02, clickOpacity
    pass_py('Clicks pass through the covering fade and walking starts behind it')
    page.wait("Number(getComputedStyle(document.querySelector('.station-opening-fade')).opacity)<0.75", timeout=8)
    mid = opacity()
    assert 0.4 < mid < 0.75, mid
    # Stage the exact mid-fade frame (opacity .5, no transition) for a stable capture;
    # the real animated timeline is already proven by the sampler and the WAAPI timing.
    page.evaluate("(()=>{const f=document.querySelector('.station-opening-fade');f.style.transition='none';f.style.opacity='0.5';})()")
    capture('mid', 1440, 810)
    page.evaluate("(()=>{const f=document.querySelector('.station-opening-fade');f.style.transition='';f.style.opacity='';})()")
    cx0 = float(page.evaluate("Number(document.querySelector('#station-player-vision').getAttribute('cx'))"))
    page.wait(f"Number(document.querySelector('#station-player-vision').getAttribute('cx'))>={cx0}+30", timeout=25)
    page.wait("getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='0'", timeout=8)
    capture('done', 1440, 810)
    check('Walking still leads to the arrival dialogue', "document.querySelector('#station-talk').open", timeout=55)
    page.click('#station-talk-close')
    page.wait("!document.querySelector('#station-talk').open")
    page.click('#station-pause-button')
    page.wait("document.querySelector('#station-pause').open")
    page.click('#station-pause-settings')
    ready('settings')
    page.click('[data-settings-return]')
    ready('station')
    check('Returning from settings does not black out the scene again', "getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='0'")
    page.cdp.call('Emulation.setEmulatedMedia', {'features': [{'name': 'prefers-reduced-motion', 'value': 'reduce'}]})
    page.click('#station-pause-button')
    page.wait("document.querySelector('#station-pause').open")
    page.click('#station-pause-menu')
    ready('mainmenu')
    page.click('.mainmenu-choice-continue')
    ready('station')
    page.wait("getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='0'", timeout=3)
    check('Reduced motion reveals the scene without the slow fade', "getComputedStyle(document.querySelector('.station-opening-fade')).transitionDuration==='0s'")
    capture('done', 844, 475)
    check('The fade still covers the whole stage at a smaller window', "(()=>{const f=document.querySelector('.station-opening-fade').getBoundingClientRect(),v=document.querySelector('[data-view=station]').getBoundingClientRect();return Math.abs(f.width-v.width)<1&&Math.abs(f.height-v.height)<1&&getComputedStyle(document.querySelector('.station-opening-fade')).opacity==='0';})()")
    check('No runtime errors during the opening', "window.__fadeErrors.length===0")
except Exception as error:
    report = {
        'date': time.strftime('%Y-%m-%d %H:%M:%S'),
        'result': 'failed',
        'error': repr(error),
        'checks': checks,
        'screens': screens,
    }
    (ROOT/'docs/station-opening-fade-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    raise
finally:
    page.close()

report = {
    'date': time.strftime('%Y-%m-%d %H:%M:%S'),
    'result': 'passed',
    'checks': checks,
    'naturalEntryTiming': timing,
    'opacitySamples': {'black': 1, 'atClick': clickOpacity, 'midFade': mid, 'final': 0},
    'style': {'transitionDuration': '2.2s', 'transitionDelay': '0.5s', 'zIndex': 1200, 'pointerEvents': 'none'},
    'screens': screens,
    'scope': '固定工作 Google Chrome + 直接 CDP 9223，隔离内存存档与静音输出；黑屏截图与半透明中间帧为同元素定格摆拍，真实动画时间线由 naturalEntryTiming 逐帧采样与 WAAPI 参数检查证明。未直接检查 Electron 工作台窗口，黑屏时长与淡入节奏待用户评价。',
}
(ROOT/'docs/station-opening-fade-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('Report docs/station-opening-fade-verification.json', flush=True)
