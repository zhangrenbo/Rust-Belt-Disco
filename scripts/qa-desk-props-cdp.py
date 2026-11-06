"""Inspect desk stationery and key removal in an isolated background Chrome target."""
import json
import os
import time
import argparse
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
screens, checks, errors = [], [], []
parser = argparse.ArgumentParser()
parser.add_argument('--view', choices=['menu', 'settings'], required=True)
parser.add_argument('--width', type=int, choices=[1440, 844], default=1440)
options = parser.parse_args()
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
        page.add_init_script('''(() => {
          const values = new Map([['northeast-disco.settings.v1','{"music":0,"filmMotion":false}']]);
          Object.defineProperty(window, 'localStorage', {value: {
            getItem:k=>values.get(String(k))??null, setItem:(k,v)=>values.set(String(k),String(v)),
            removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),key:i=>[...values.keys()][i]??null,
            get length(){return values.size;}
          }});
        })();''')
        page.on('pageerror', lambda error: errors.append(str(error)))
        cdp = context.new_cdp_session(page)
        cdp.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        try:
            cdp.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle:', str(error)[:120], flush=True)
        page.emulate_media(reduced_motion='reduce')
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.goto(f'http://127.0.0.1:5173/?desk-props-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.wait_for_function('!!document.querySelector("[data-view=mainmenu].active")')

        def capture(name, width, height):
            page.wait_for_function("[...document.querySelectorAll('.view.active img')].filter(i=>i.getAttribute('src')).every(i=>i.complete&&i.naturalWidth>0)")
            page.wait_for_function("getComputedStyle(document.querySelector('.view.active')).opacity==='1'")
            page.mouse.move(width-3, height-3)
            for w in [width+1, width]:
                cdp.send('Emulation.setDeviceMetricsOverride', {'width': w, 'height': height, 'deviceScaleFactor': 1, 'mobile': False})
            path = ROOT / f'docs/{name}-{width}.png'
            page.screenshot(path=str(path), timeout=12000)
            screens.append({'name': name, 'viewport': [width, height], 'path': str(path)})
            print('Captured', path.name, flush=True)

        for width, height in [(options.width, 810 if options.width == 1440 else 475)]:
            page.set_viewport_size({'width': width, 'height': height})
            props = page.locator('.event-desk-stationery')
            page.wait_for_function('''async () => {
              const e=document.querySelector('.event-desk-stationery');
              const url=getComputedStyle(e,'::before').backgroundImage.slice(5,-2);
              const i=new Image(); i.src=url; await i.decode(); return i.naturalWidth>0;
            }''')
            check = page.evaluate('''() => {
              const p=document.querySelector('.event-desk-stationery'),r=p.getBoundingClientRect(),stage=document.querySelector('#app').getBoundingClientRect();
              return {width:innerWidth, props:r.toJSON(),bounded:r.width<stage.width*.15&&r.height<stage.height*.2,
                noninteractive:getComputedStyle(p).pointerEvents==='none',
                background:document.querySelector('.event-desk').getAttribute('src'),
                menuReachable:[...document.querySelectorAll('.mainmenu-actions button')].filter(e=>e.getClientRects().length&&!e.disabled).every(e=>{const r=e.getBoundingClientRect();return e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))})};
            }''')
            assert check['bounded'] and check['noninteractive'] and check['menuReachable'], check
            assert check['background'].endswith('event-desk-bg-medal-marriage-v2.png'), check
            checks.append(check)
            if options.view == 'menu':
                capture('menu-stationery', width, height)
            page.locator('.mainmenu-actions [data-open-view=settings]').click()
            image = page.locator('.settings-dossier-art')
            assert image.get_attribute('src').endswith('settings-dossier-art-v5-no-key.png')
            assert image.evaluate('e=>getComputedStyle(e).clipPath') != 'none'
            if options.view == 'settings':
                capture('settings-no-key', width, height)
            page.locator('[data-music-library-open]').click()
            page.locator('[data-music-library-close]').click()
            page.locator('[data-settings-return]').click()
            assert page.locator('[data-view=mainmenu]').evaluate('e=>e.classList.contains("active")')
        assert not errors, errors
        (ROOT/f'docs/desk-props-{options.view}-{options.width}-verification.json').write_text(json.dumps({
            'transport':'fixed work Chrome direct CDP 9223; task-created background target',
            'storage':'isolated in-memory settings; real player data untouched',
            'checks':checks,'screens':screens,'errors':errors,'humanArtReview':'pending'
        }, ensure_ascii=False, indent=2)+'\n',encoding='utf-8')
    finally:
        page.close()
