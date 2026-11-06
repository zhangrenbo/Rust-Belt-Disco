"""Check the real game through fixed work Chrome CDP; never use player storage."""
import json
import base64
import os
import time
import urllib.request
import websocket
from contextlib import closing
from pathlib import Path
from urllib.parse import quote

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
checks, screens, errors, texture_measurements = [], [], [], []

with sync_playwright() as playwright:
    browser = playwright.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    browser_cdp = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        target = browser_cdp.send('Target.createTarget', {'url': 'about:blank', 'background': True})['targetId']
    page = created.value
    try:
        page.set_default_timeout(10000)
        # Isolate this disposable QA page from source edits and concurrent HMR reloads.
        page.route('**/@vite/client', lambda route: route.fulfill(status=200, content_type='application/javascript', body=''))
        cdp = context.new_cdp_session(page)
        assert cdp.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
        try:
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle activation:', str(error)[:160])
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.add_init_script('''(() => {
          const seed = new URLSearchParams(location.search).get('settings') || '{"music":0}';
          const values = new Map([['northeast-disco.settings.v1', seed], ['qa-unrelated', 'preserve']]);
          Object.defineProperty(window, 'localStorage', { value: {
            getItem: key => values.get(String(key)) ?? null,
            setItem: (key, value) => values.set(String(key), String(value)),
            removeItem: key => values.delete(String(key)),
            clear: () => values.clear(), key: i => [...values.keys()][i] ?? null,
            get length() { return values.size; }
          }});
        })();''')

        def navigate(settings=None):
            query = f'?retro-qa={time.time_ns()}'
            if settings is not None:
                query += '&settings=' + quote(settings)
            page.goto('http://127.0.0.1:5173/' + query, wait_until='domcontentloaded')
            page.wait_for_function('document.querySelector("#app").dataset.film !== undefined')

        def check(name, predicate):
            page.wait_for_function(predicate, polling=100, timeout=5000)
            checks.append(name)
            if len(checks) % 4 == 0:
                print(f'{len(checks)} checks passed: {name}', flush=True)

        def capture(name, width=1440, height=810):
            page.set_viewport_size({'width': width, 'height': height})
            page.wait_for_function('''() => [...document.querySelectorAll('.view.active img')]
              .filter(i => i.getAttribute('src')).every(i => i.complete && i.naturalWidth > 0)''')
            page.wait_for_function('''() => getComputedStyle(document.querySelector('.view.active')).opacity === '1' &&
              getComputedStyle(document.querySelector('.page-shutter')).opacity === '0' ''', polling=100)
            page.mouse.move(width-2, height-2)
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
            # A static background target may have no fresh surface after a settings switch.
            for surface_width in (width + 1, width):
                cdp.send('Emulation.setDeviceMetricsOverride', {'width': surface_width, 'height': height, 'deviceScaleFactor': 1, 'mobile': False})
            # Direct CDP screenshot with an explicit socket deadline; no foreground activation.
            opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
            with opener.open('http://127.0.0.1:9223/json/list', timeout=5) as response:
                metadata = next(t for t in json.load(response) if t['id'] == target)
            with closing(websocket.create_connection(metadata['webSocketDebuggerUrl'], timeout=10, suppress_origin=True)) as connection:
                connection.send(json.dumps({'id': 1, 'method': 'Page.captureScreenshot', 'params': {'format': 'png', 'fromSurface': True, 'captureBeyondViewport': False}}))
                frame = json.loads(connection.recv())
                assert 'error' not in frame, frame.get('error')
                frame = frame['result']
            (ROOT / f'docs/retro-film-dense-snow-{name}-{width}.png').write_bytes(base64.b64decode(frame['data']))
            screens.append({'name': name, 'viewport': [width, height]})

        def measure_motion(name, selector):
            size = page.viewport_size
            capture('texture-' + name, size['width'], size['height'])
            page.mouse.move(size['width']/2, size['height']/2)
            samples = []
            started = time.monotonic()
            detail_saved = False
            while time.monotonic() - started < (8 if name == 'mainmenu' else 2.5):
                sample = page.evaluate('''selector => {
                  const art=document.querySelector(selector), c=getComputedStyle(art), r=art.getBoundingClientRect();
                  const damage=document.querySelector('.projector-edge-snow');
                  return {translate:c.translate,scale:c.scale,x:r.x,y:r.y,
                    grain:getComputedStyle(document.querySelector('.grain')).backgroundPosition,
                    overlay:getComputedStyle(damage).translate,
                    dust:damage.querySelectorAll('.film-dust:not([hidden]),.film-fiber:not([hidden])').length,
                    scratches:damage.querySelectorAll('.film-scratch:not([hidden])').length};
                }''', selector)
                samples.append(sample)
                if name == 'mainmenu' and sample['scratches'] and not detail_saved:
                    # Freeze the real sampled damage frame for visual inspection only.
                    page.evaluate('''() => {
                      const source=document.querySelector('.projector-edge-snow');
                      const still=source.cloneNode(true); still.id='qa-damage-still';
                      still.style.translate=getComputedStyle(source).translate;
                      source.style.visibility='hidden'; source.after(still);
                    }''')
                    try:
                        page.screenshot(path=str(ROOT / 'docs/retro-film-dense-damage-detail.png'), timeout=5000)
                    finally:
                        page.evaluate('''() => {
                          document.querySelector('#qa-damage-still')?.remove();
                          document.querySelector('.projector-edge-snow').style.removeProperty('visibility');
                        }''')
                    detail_saved = True
                time.sleep(.055)
            grain_frames = len(set(s['grain'] for s in samples))
            overlay_frames = len(set(s['overlay'] for s in samples))
            assert grain_frames >= 4 and overlay_frames >= 4, (name, grain_frames, overlay_frames)
            assert all(s['translate']=='none' and s['scale']=='none' for s in samples), name
            dust_frames=sum(s['dust']>0 for s in samples)
            scratch_frames=sum(s['scratches']>0 for s in samples)
            assert dust_frames > 0, name
            if name == 'mainmenu':
                assert scratch_frames > 0, 'Expected an actual scratch during the observation window'
                assert max(s['x'] for s in samples)-min(s['x'] for s in samples)<.02
                assert max(s['y'] for s in samples)-min(s['y'] for s in samples)<.02
            texture_measurements.append({'view':name,'artworkStable':True,'grainFrames':grain_frames,
                                         'overlayFrames':overlay_frames,'dustFrames':dust_frames,'scratchFrames':scratch_frames})
            print(f'Texture verified: {name}; artwork fixed, {grain_frames} grain changes, dust={dust_frames}, scratches={scratch_frames}',flush=True)

        def measure_cadence(name):
            measured = page.evaluate('''() => new Promise(resolve => {
              const stage=document.querySelector('#app'), damage=document.querySelector('.projector-edge-snow');
              const times=[], counts=[]; let last='';
              const observer=new MutationObserver(()=>{
                const position=stage.style.getPropertyValue('--film-grain-x')+stage.style.getPropertyValue('--film-grain-y');
                if(position===last)return;last=position;
                times.push(performance.now());counts.push(damage.querySelectorAll('.film-dust:not([hidden])').length);
              });
              observer.observe(stage,{attributes:true,attributeFilter:['style']});
              setTimeout(()=>{observer.disconnect();const gaps=times.slice(1).map((t,i)=>t-times[i]);
                resolve({frames:times.length,meanIntervalMs:gaps.reduce((a,b)=>a+b,0)/gaps.length,
                  meanVisibleDust:counts.reduce((a,b)=>a+b,0)/counts.length,
                  peakVisibleDust:Math.max(...counts),dustNodes:damage.querySelectorAll('.film-dust').length});
              },3500);
            })''')
            assert measured['dustNodes']==48 and measured['meanVisibleDust']>=25,measured
            assert measured['frames']>= (18 if name=='balanced' else 29),measured
            texture_measurements.append({'quality':name,**measured})
            print('Measured snow cadence:',name,json.dumps(measured),flush=True)

        def settings():
            page.locator('.mainmenu-actions [data-open-view="settings"]').click()

        def apply():
            page.locator('[data-save-settings]').click()
            page.wait_for_function('!document.querySelector("[data-save-settings]").disabled')

        def back():
            page.locator('[data-settings-return]').click()

        page.set_viewport_size({'width': 1440, 'height': 810})
        page.emulate_media(reduced_motion='no-preference')
        navigate()
        check('old settings gain the 65% default without losing mute', '''() =>
          document.querySelector('#app').dataset.film === 'on' &&
          document.querySelector('[data-film-strength]').value === '65' &&
          document.querySelector('[data-audio-channel="music"]').value === '0' ''')
        check('colour pass is active; stage geometry has no global filter', '''() =>
          getComputedStyle(document.querySelector('.projector-film')).backdropFilter.includes('sepia') &&
          getComputedStyle(document.querySelector('#app')).filter === 'none' ''')
        capture('on-menu')
        measure_cadence('enhanced')
        measure_motion('mainmenu', '.event-center-menu')
        settings()
        page.locator('[data-film-toggle]').click()
        check('switch immediately previews off and disables strength', '''() =>
          document.querySelector('#app').dataset.film === 'off' && document.querySelector('[data-film-strength]').disabled &&
          ['.grain','.projector-film','.projector-edge-snow','.projector-edge-flicker'].every(s => getComputedStyle(document.querySelector(s)).display === 'none')''')
        back()
        check('leaving without applying cancels the preview', 'document.querySelector("#app").dataset.film === "on"')
        settings()
        page.locator('[data-film-strength]').press('Home')
        check('zero is a complete bypass', 'document.querySelector("#app").dataset.film === "off"')
        apply()
        saved_zero = page.evaluate('localStorage.getItem("northeast-disco.settings.v1")')
        back()
        capture('off-menu')
        navigate(saved_zero)
        check('zero survives a fresh page load', 'document.querySelector("#app").dataset.film === "off"')
        settings()
        page.locator('[data-film-strength]').press('End')
        check('maximum strength activates the effect', 'document.querySelector("#app").style.getPropertyValue("--film-strength") === "1"')
        page.locator('button[data-film-motion]').click()
        apply()
        check('motion can stop while colour remains', '''() =>
          document.querySelector('#app').dataset.film === 'on' && document.querySelector('#app').dataset.filmMotion === 'off' &&
          getComputedStyle(document.querySelector('.grain')).animationName === 'none' ''')
        saved_static = page.evaluate('localStorage.getItem("northeast-disco.settings.v1")')
        navigate(saved_static)
        check('strength and disabled motion survive reload', '''() =>
          document.querySelector('[data-film-strength]').value === '100' &&
          document.querySelector('#app').dataset.filmMotion === 'off' ''')
        settings()
        page.locator('[data-reset-settings]').click()
        # Keep this QA page silent after resetting defaults.
        page.locator('[data-audio-channel="music"]').press('Home')
        apply()
        check('reset restores film defaults', '''() => document.querySelector('[data-film-strength]').value === '65' &&
          document.querySelector('[data-film-toggle]').getAttribute('aria-checked') === 'true' ''')
        page.locator('button[data-quality="performance"]').click()
        apply()
        check('performance keeps colour and drops grain and motion', '''() =>
          document.querySelector('#app').dataset.quality === 'performance' &&
          getComputedStyle(document.querySelector('.projector-film')).display === 'block' &&
          getComputedStyle(document.querySelector('.grain')).display === 'none' &&
          document.querySelector('#app').dataset.filmMotion === 'off' ''')
        page.locator('button[data-quality="balanced"]').click()
        apply()
        check('balanced keeps animated film texture without moving artwork', '''() =>
          getComputedStyle(document.querySelector('.projector-edge-snow')).display === 'block' &&
          getComputedStyle(document.querySelector('.settings-dossier-art')).translate === 'none' ''')
        measure_cadence('balanced')
        page.locator('button[data-quality="enhanced"]').click()
        apply()
        page.emulate_media(reduced_motion='reduce')
        check('system reduced motion overrides film animation', '''() =>
          document.querySelector('#app').dataset.filmMotion === 'off' &&
          getComputedStyle(document.querySelector('.grain')).animationName === 'none' ''')
        page.emulate_media(reduced_motion='no-preference')
        check('motion resumes when system preference allows it', 'document.querySelector("#app").dataset.filmMotion === "on"')
        measure_motion('settings', '.settings-dossier-art')
        check('settings text stays still', 'getComputedStyle(document.querySelector(".settings-dossier-form")).translate === "none"')
        page.evaluate('''() => {
          Object.defineProperty(document, 'hidden', { configurable:true, value:true });
          document.dispatchEvent(new Event('visibilitychange'));
        }''')
        check('hidden page disables film animation', 'document.querySelector("#app").dataset.filmMotion === "off"')
        page.evaluate('''() => {
          delete document.hidden;
          document.dispatchEvent(new Event('visibilitychange'));
        }''')
        for width, height in [(1440, 810), (844, 475)]:
            capture('settings', width, height)
            check(f'settings controls reachable at {width}', '''() =>
              [...document.querySelectorAll('.settings-film-controls button, .settings-film-controls input, [data-save-settings], [data-reset-settings]')]
              .every(el => {const r=el.getBoundingClientRect();return r.width>0 && r.height>0 && el.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2));})''')
        back()
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.locator('.mainmenu-actions [data-open-view="entry"]').click()
        measure_motion('entry', '.entry-directory')
        page.locator('[data-view="entry"] [data-open-view="mainmenu"]').click()
        page.locator('.mainmenu-choice-start').click()
        measure_motion('character-select', '[data-character-world]')
        page.locator('[data-character-choice="大厦崩塌"]').click()
        check('character selection still leads to the UI-only scene', 'document.querySelector(".view.active").dataset.view === "ui-only"')
        page.locator('#ui-only-notice button').click()
        check('UI-only return remains usable with the filter active', 'document.querySelector(".view.active").dataset.view === "mainmenu"')
        check('unrelated stored data preserved', 'localStorage.getItem("qa-unrelated") === "preserve"')
        assert not errors, errors
        report = {'transport': 'fixed work Google Chrome + direct CDP 9223', 'browser': browser.version,
                  'storage': 'isolated page memory; reload seeded from actual applied JSON; player saves untouched',
                  'checks': checks, 'screens': screens, 'errors': errors, 'humanArtApproval': 'pending',
                  'textureMeasurements': texture_measurements,
                  'performance': 'quality fallbacks verified; target-device FPS not benchmarked'}
        (ROOT / 'docs/retro-film-dense-snow-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'checks': len(checks), 'screens': len(screens), 'errors': errors}))
    finally:
        page.close()
