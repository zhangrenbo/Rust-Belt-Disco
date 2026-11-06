"""Dedicated background Chrome CDP target, isolated storage and silent output sink."""
import base64
import json
import os
import sys
import time
import urllib.request
from contextlib import closing
from pathlib import Path

import websocket
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
checks, screens, errors = [], [], []
appearance_only = '--appearance' in sys.argv
INIT = r'''(() => {
  const values = new Map([['northeast-disco.settings.v1','{"music":54}'],['qa-unrelated','preserve']]);
  Object.defineProperty(window,'localStorage',{value:{
    getItem:k=>values.get(String(k))??null,setItem:(k,v)=>values.set(String(k),String(v)),
    removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),key:i=>[...values.keys()][i]??null,
    get length(){return values.size}
  }});
  const Native = window.AudioContext;
  window.AudioContext = class extends Native {
    constructor(...args) {
      super(...args);
      const gain=this.createGain.bind(this), source=this.createBufferSource.bind(this);
      const analyser=this.createAnalyser(); analyser.fftSize=2048;
      const sink=gain(); sink.gain.value=0; analyser.connect(sink); sink.connect(this.destination);
      const qa={context:this, analyser, sources:[], master:null};
      (window.__audioQA ??= []).push(qa);
      window.__recordQA ??= qa; // Music remains the first context; paper SFX has its own silent analyser.
      this.createGain=()=>{
        const node=gain();
        if(!qa.master){
          qa.master=node; const connect=node.connect.bind(node);
          node.connect=(dest,...rest)=>connect(dest===this.destination?analyser:dest,...rest);
        }
        return node;
      };
      this.createBufferSource=()=>{
        const node=source(), entry={node,offset:0,stopped:false};
        const start=node.start.bind(node), stop=node.stop.bind(node);
        node.start=(when,offset=0)=>{entry.offset=offset;start(when,offset)};
        node.stop=(...args)=>{entry.stopped=true;stop(...args)};
        qa.sources.push(entry); return node;
      };
      qa.rms=()=>{const data=new Float32Array(analyser.fftSize);analyser.getFloatTimeDomainData(data);return Math.sqrt(data.reduce((sum,x)=>sum+x*x,0)/data.length)};
    }
  };
})();'''

with sync_playwright() as p:
    browser = p.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        target = bc.send('Target.createTarget', {'url': 'about:blank', 'background': True})['targetId']
    page = created.value
    try:
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda route: route.fulfill(status=200,content_type='application/javascript',body=''))
        cdp = context.new_cdp_session(page)
        try:
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle:', str(error)[:120])
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        page.add_init_script(INIT)
        page.on('pageerror', lambda error: errors.append(str(error)))

        def check(name, expression):
            page.wait_for_function(expression, polling=100, timeout=8000)
            checks.append(name)
            print(f'{len(checks)} PASS {name}', flush=True)

        def capture(name, width=1440, height=810):
            page.set_viewport_size({'width':width,'height':height})
            page.wait_for_function("[...document.querySelectorAll('.view.active img')].every(i=>i.complete&&i.naturalWidth>0)")
            page.wait_for_function("getComputedStyle(document.querySelector('.view.active')).opacity==='1'", polling=100)
            page.mouse.move(width-2,height-2)
            for w in (width+1,width):
                cdp.send('Emulation.setDeviceMetricsOverride', {'width':w,'height':height,'deviceScaleFactor':1,'mobile':False})
            opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
            with opener.open('http://127.0.0.1:9223/json/list',timeout=5) as response:
                metadata=next(t for t in json.load(response) if t['id']==target)
            with closing(websocket.create_connection(metadata['webSocketDebuggerUrl'],timeout=10,suppress_origin=True)) as ws:
                ws.send(json.dumps({'id':1,'method':'Page.captureScreenshot','params':{'format':'png','fromSurface':True}}))
                result=json.loads(ws.recv())
                assert 'error' not in result, result.get('error')
            path=f'docs/record-library-{"light-" if appearance_only else ""}{name}-{width}.png'
            (ROOT/path).write_bytes(base64.b64decode(result['result']['data']))
            screens.append({'path':path,'viewport':[width,height]})

        page.set_viewport_size({'width':1440,'height':810})
        page.goto(f'http://127.0.0.1:5173/?record-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.locator('.mainmenu-actions [data-open-view="settings"]').click()
        page.locator('[data-music-library-open]').click()
        check('theme audio decoded and playing', "document.querySelector('#app').dataset.musicState==='playing' && window.__recordQA.rms()>.001")
        check('reference turntable and real two-track catalog visible', "document.querySelectorAll('.record-track').length===2 && document.querySelector('.record-tonearm img').naturalWidth>0 && getComputedStyle(document.querySelector('.settings-dossier-art')).display==='none'")
        capture('playing')
        if appearance_only:
            check('new paper artwork loads with the existing silhouette', '''async () => {
              const paper=document.querySelector('.record-catalog-paper');
              const url=getComputedStyle(paper).backgroundImage.slice(5,-2);
              const image=new Image(); image.src=url; await image.decode();
              return image.naturalWidth===1122&&image.naturalHeight===1402&&getComputedStyle(paper).maskMode==='luminance';
            }''')
            page.locator('[data-record-play]').click()
            check('lit controls still pause real playback', "document.querySelector('#app').dataset.musicState==='paused'")
            page.locator('[data-record-play]').hover()
            check('button hover keeps its own material and remains reachable', '''() => {
              const b=document.querySelector('[data-record-play]'),r=b.getBoundingClientRect();
              return b.matches(':hover')&&b.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));
            }''')
            page.locator('[data-record-play]').click()
            check('lit controls resume real playback', "document.querySelector('#app').dataset.musicState==='playing'")
            capture('compact',844,475)
            check('all compact controls remain unobstructed under the light layer', '''() => [...document.querySelectorAll('.record-library button,.record-library input')].filter(e=>!e.disabled&&e.getClientRects().length).every(e=>{const r=e.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return x>=0&&y>=0&&x<innerWidth&&y<innerHeight&&e.contains(document.elementFromPoint(x,y))})''')
            check('catalog text fits its writing area', '''() => [...document.querySelectorAll('.record-track-main,.record-catalog-heading,.record-catalog-footer')].every(e=>e.scrollWidth<=e.clientWidth+1)''')
            page.locator('[data-music-library-close]').click()
            check('return restores normal settings without a light overlay', "document.querySelector('#settings-music-library').hidden && !document.querySelector('[data-view=settings]').classList.contains('is-record-library')")
            assert not errors,errors
            (ROOT/'docs/record-library-light-verification.json').write_text(json.dumps({'checks':checks,'screenshots':screens,'errors':errors,'humanArtReview':'pending'},ensure_ascii=False,indent=2),encoding='utf-8')
            print(f'COMPLETE {len(checks)} appearance checks, {len(screens)} screenshots',flush=True)
            sys.exit(0)
        start = page.locator('[data-record-seek]').input_value()
        page.wait_for_function('start => Number(document.querySelector("[data-record-seek]").value)>Number(start)+.4',arg=start,polling=100)
        check('record rotation follows playback', "getComputedStyle(document.querySelector('.record-vinyl')).animationPlayState==='running'")
        page.locator('[data-record-play]').click()
        check('pause stops real audio and record', "document.querySelector('#app').dataset.musicState==='paused' && window.__recordQA.rms()<.000001 && getComputedStyle(document.querySelector('.record-vinyl')).animationPlayState==='paused'")
        paused = page.locator('[data-record-seek]').input_value()
        page.wait_for_timeout(350)
        assert page.locator('[data-record-seek]').input_value()==paused
        slider=page.locator('[data-record-seek]')
        box=slider.bounding_box()
        page.mouse.click(box['x']+box['width']*.4,box['y']+box['height']/2)
        check('pointer seek preserves pause and updates real position', "document.querySelector('#app').dataset.musicState==='paused' && Number(document.querySelector('[data-record-seek]').value)>85 && Number(document.querySelector('[data-record-seek]').value)<110")
        seeked=float(slider.input_value())
        page.locator('[data-record-play]').click()
        check('resume starts source from seek position', f"document.querySelector('#app').dataset.musicState==='playing' && Math.abs(window.__recordQA.sources.at(-1).offset-{seeked})<1")
        slider.focus(); slider.press('ArrowRight')
        check('keyboard seek controls audio source', f"window.__recordQA.sources.at(-1).offset>{seeked}")
        page.locator('[data-record-next]').click()
        check('next loads actual station audio', "document.querySelector('#app').dataset.musicTrack==='station' && document.querySelector('#app').dataset.musicState==='playing' && window.__recordQA.sources.at(-1).node.buffer.duration<60")
        page.locator('[data-record-previous]').click()
        check('previous restores theme without refetching decoder', "document.querySelector('#app').dataset.musicTrack==='menu' && document.querySelector('#app').dataset.musicState==='playing'")
        page.locator('[data-record-favorites-only]').check()
        check('empty favorites has honest state and disabled skip', "!document.querySelector('[data-record-empty]').hidden && document.querySelector('[data-record-next]').disabled")
        capture('empty')
        page.locator('[data-record-favorite]').click()
        check('favorite persists independently and filter updates', "JSON.parse(localStorage.getItem('northeast-disco.music-favorites.v1')).includes('menu') && document.querySelectorAll('.record-track:not([hidden])').length===1 && document.querySelector('[data-record-empty]').hidden")
        page.locator('[data-record-favorites-only]').uncheck()
        page.locator('[data-record-repeat]').click()
        check('repeat switch changes source looping', "!window.__recordQA.sources.at(-1).node.loop && document.querySelector('[data-record-repeat]').getAttribute('aria-pressed')==='false'")
        # Real source end: seek within a fraction of a second of the actual buffer end.
        slider.focus(); slider.press('End')
        check('nonlooping track actually ends', "document.querySelector('#app').dataset.musicState==='ended'")
        page.locator('[data-record-play]').click()
        check('play after end starts from beginning', "document.querySelector('#app').dataset.musicState==='playing' && window.__recordQA.sources.at(-1).offset===0")
        page.emulate_media(reduced_motion='reduce')
        check('system reduced motion stops record rotation', "getComputedStyle(document.querySelector('.record-vinyl')).animationName==='none'")
        page.emulate_media(reduced_motion='no-preference')
        capture('compact',844,475)
        check('compact controls stay visible and unobstructed', '''() => [...document.querySelectorAll('.record-library button,.record-library input')].filter(e=>!e.disabled&&e.getClientRects().length).every(e=>{const r=e.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2;return x>=0&&y>=0&&x<innerWidth&&y<innerHeight&&e.contains(document.elementFromPoint(x,y))})''')
        page.locator('[data-record-next]').click()
        check('compact next remains usable', "document.querySelector('#app').dataset.musicTrack==='station'")
        page.locator('[data-music-library-close]').click()
        check('return restores original theme and settings', "document.querySelector('#settings-music-library').hidden && !document.querySelector('[data-settings-controls]').hidden && document.querySelector('#app').dataset.musicTrack==='menu' && window.__recordQA.sources.at(-1).node.loop")
        capture('settings',844,475)
        # Saved mute and unsaved settings must survive a round trip through the player.
        page.evaluate("window.dispatchEvent(new CustomEvent('disco:audio-settings',{detail:{music:0}}));document.querySelector('[data-audio-channel=music]').value='31'")
        page.locator('[data-music-library-open]').click()
        page.locator('[data-record-next]').click()
        check('mute survives track switches', "window.__recordQA.master.gain.value===0 && document.querySelector('#app').dataset.musicState==='muted'")
        page.keyboard.press('Escape')
        check('Escape returns settings without losing draft or mute', "document.querySelector('#settings-music-library').hidden && document.querySelector('[data-audio-channel=music]').value==='31' && window.__recordQA.master.gain.value===0")
        check('unrelated storage untouched', "localStorage.getItem('qa-unrelated')==='preserve'")
        assert not errors, errors
        (ROOT/'docs/record-library-verification.json').write_text(json.dumps({'checks':checks,'screenshots':screens,'errors':errors,'audio':'Web Audio analyser before a QA-only silent output sink; physical speaker not inspected','humanArtReview':'pending'},ensure_ascii=False,indent=2),encoding='utf-8')
        print(f'COMPLETE {len(checks)} checks, {len(screens)} screenshots',flush=True)
    finally:
        page.close()
