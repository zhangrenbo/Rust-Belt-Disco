"""Historical preview/cancel volume QA (2026-09-13).

Volume now autosaves. Run qa-volume-autosave-cdp.py for current persistence behavior;
this script preserves the original immediate-playback regression and old assertions.
"""
import ast
import json
import os
import re
import time
import zipfile
from pathlib import Path
from urllib.parse import quote

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
tree = ast.parse((ROOT / 'scripts/qa-record-library-cdp.py').read_text(encoding='utf-8'))
init = next(ast.literal_eval(node.value) for node in tree.body if isinstance(node, ast.Assign)
            and any(getattr(target, 'id', None) == 'INIT' for target in node.targets))
init = init.replace("'{\"music\":54}'", "(new URLSearchParams(location.search).get('settings') || '{\"music\":54}')")
checks, errors = [], []
phase = 'before'
with zipfile.ZipFile(ROOT / 'docs/volume-control-before-20260913.zip') as archive:
    previous = archive.read('scene.js').decode('utf-8')

with sync_playwright() as pw:
    browser = pw.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        bc.send('Target.createTarget', {'url': 'about:blank', 'background': True})
    page = created.value
    try:
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda route: route.fulfill(status=200, content_type='application/javascript', body=''))
        page.route(re.compile(r'/scene\.js(?:\?.*)?$'), lambda route: route.fulfill(status=200, content_type='application/javascript', body=previous)
                   if phase == 'before' else route.continue_())
        page.add_init_script(init)
        page.on('pageerror', lambda error: errors.append(str(error)))
        cdp = context.new_cdp_session(page)
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        try:
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle:', str(error)[:150], flush=True)
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.emulate_media(reduced_motion='reduce')
        slider = page.locator('[data-audio-channel=music]')

        def navigate(settings=None):
            suffix = '&settings=' + quote(settings) if settings else ''
            page.goto(f'http://127.0.0.1:5173/?volume-qa={time.time_ns()}{suffix}', wait_until='domcontentloaded')
            if page.locator('#ui-only-notice[open]').count():
                page.locator('#ui-only-notice button').click()
            page.locator('.mainmenu-actions [data-open-view=settings]').click()
            page.wait_for_function('window.__recordQA?.context.state === "running"')

        def measure():
            return page.evaluate('''({slider:Number(document.querySelector('[data-audio-channel=music]').value),
              gain:window.__recordQA.master.gain.value,rms:window.__recordQA.rms(),
              state:document.querySelector('#app').dataset.musicState,
              track:document.querySelector('#app').dataset.musicTrack,
              sourceCount:window.__recordQA.sources.length})''')

        def check(name, expression):
            page.wait_for_function(expression, polling=80, timeout=8000)
            checks.append({'check': name, **measure()})
            print(f'{len(checks)} PASS {name}', flush=True)

        def key(value):
            slider.focus()
            slider.press(value)

        navigate()
        page.wait_for_function('window.__recordQA.rms() > .001')
        before = [measure()]
        key('End'); page.wait_for_timeout(250); before.append(measure())
        key('Home'); page.wait_for_timeout(250); before.append(measure())
        assert before[-1]['slider'] == 0 and before[-1]['gain'] > .5 and before[-1]['rms'] > .0001, before
        (ROOT / 'docs/volume-control-repro.json').write_text(json.dumps({
            'reproduced': True, 'source': 'backed-up scene.js, injected only into test page', 'readings': before
        }, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

        phase = 'after'
        navigate()
        check('saved initial volume drives real audio', 'Math.abs(window.__recordQA.master.gain.value-.54)<.001 && window.__recordQA.rms()>.001')
        count = measure()['sourceCount']
        key('End')
        check('keyboard volume changes sound before Apply', 'window.__recordQA.master.gain.value>.999 && window.__recordQA.rms()>.001')
        key('Home')
        check('zero immediately silences the real signal', 'window.__recordQA.master.gain.value===0 && window.__recordQA.rms()<1e-7')
        box = slider.bounding_box()
        page.mouse.click(box['x'] + box['width'] * .3, box['y'] + box['height'] / 2)
        value = int(slider.input_value())
        assert 25 <= value <= 35, value
        check('pointer volume matches actual output gain', f'Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001 && window.__recordQA.rms()>1e-6')
        assert measure()['sourceCount'] == count, 'volume must not restart the music'
        check('preview leaves saved settings unchanged', 'JSON.parse(localStorage.getItem("northeast-disco.settings.v1")).music===54')
        page.locator('[data-music-library-open]').click()
        page.locator('[data-record-next]').click()
        check('record library uses the same draft volume on another track', f'document.querySelector("#app").dataset.musicTrack==="station" && Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001 && window.__recordQA.rms()>1e-6')
        page.locator('[data-record-play]').click()
        check('record pause silences playback', 'document.querySelector("#app").dataset.musicState==="paused" && window.__recordQA.rms()<1e-7')
        page.locator('[data-record-play]').click()
        check('record resume preserves shared volume', f'document.querySelector("#app").dataset.musicState==="playing" && Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001 && window.__recordQA.rms()>1e-6')
        page.locator('[data-music-library-close]').click()
        key('Home')
        page.locator('[data-music-library-open]').click()
        page.locator('[data-record-next]').click()
        check('record track changes preserve zero volume', 'document.querySelector("#app").dataset.musicState==="muted" && window.__recordQA.master.gain.value===0 && window.__recordQA.rms()<1e-7')
        page.locator('[data-music-library-close]').click()
        page.locator('[data-save-settings]').click()
        check('Apply saves shared mute', 'JSON.parse(localStorage.getItem("northeast-disco.settings.v1")).music===0')
        key('End')
        page.locator('[data-settings-return]').click()
        check('cancel restores saved audio and slider together', 'window.__recordQA.master.gain.value===0 && document.querySelector("[data-audio-channel=music]").value==="0"')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        page.locator('[data-reset-settings]').click()
        check('restore defaults changes sound immediately', 'Math.abs(window.__recordQA.master.gain.value-.54)<.001 && window.__recordQA.rms()>.001 && JSON.parse(localStorage.getItem("northeast-disco.settings.v1")).music===0')
        slider.press('ArrowLeft')
        page.locator('[data-save-settings]').click()
        saved = page.evaluate('localStorage.getItem("northeast-disco.settings.v1")')
        saved_volume = json.loads(saved)['music']
        navigate(saved)
        check('saved volume is restored on a fresh page', f'Math.abs(window.__recordQA.master.gain.value-{saved_volume}/100)<.001 && Number(document.querySelector("[data-audio-channel=music]").value)==={saved_volume}')
        page.set_viewport_size({'width': 844, 'height': 475})
        box = slider.bounding_box()
        page.mouse.click(box['x'] + box['width'] * .7, box['y'] + box['height'] / 2)
        value = int(slider.input_value())
        assert 65 <= value <= 75, value
        check('compact pointer volume reaches actual audio', f'Math.abs(window.__recordQA.master.gain.value-{value}/100)<.001 && window.__recordQA.rms()>1e-6')
        check('unrelated data is preserved', 'localStorage.getItem("qa-unrelated")==="preserve"')
        assert not errors, errors
        report = {'checkedAt': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()), 'checks': checks, 'errors': errors,
                  'audio': 'Real Web Audio analyser before a QA-only silent output sink; physical speaker not tested',
                  'storage': 'Isolated in-memory settings; saved JSON seeds the fresh-page check; player saves untouched',
                  'browser': 'Fixed work Chrome via direct CDP 9223; dedicated background target only'}
        (ROOT / 'docs/volume-control-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    finally:
        page.close()
