"""Inspect the marriage booklet in the real menu using isolated memory saves."""
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'

with sync_playwright() as playwright:
    browser = playwright.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    control = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        target = control.send('Target.createTarget', {'url': 'about:blank', 'background': True})['targetId']
    page = created.value
    errors, screens = [], []
    page.on('pageerror', lambda error: errors.append(str(error)))
    try:
        page.set_default_timeout(10000)
        session = context.new_cdp_session(page)
        assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
        session.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
        try:
            session.send('Page.setWebLifecycleState', {'state': 'active'})
        except Exception as error:
            print('Lifecycle activation:', str(error)[:160])
        page.add_init_script('''(() => {
          const save = JSON.stringify({version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},inspected:[],
            position:{x:440,y:640},warningHeard:false,shadow:null,completed:false,updatedAt:'2026-09-13T00:00:00.000Z'});
          const values = new Map([['northeast-disco:campaign:v1:auto',save]]);
          Object.defineProperty(window,'localStorage',{configurable:true,value:{getItem:k=>values.get(String(k))??null,
            setItem:(k,v)=>values.set(String(k),String(v)),removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),
            key:i=>[...values.keys()][i]??null,get length(){return values.size;}}});
        })();''')
        page.set_viewport_size({'width': 1440, 'height': 810})
        page.goto(f'http://127.0.0.1:5173/?menu-marriage-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.locator('.mainmenu-choice-continue').wait_for(state='visible')
        page.wait_for_function("() => [...document.querySelectorAll('.view-mainmenu img')].every(i => i.complete && i.naturalWidth > 0)")
        page.wait_for_function("() => !document.querySelector('#app').classList.contains('is-view-switching') && document.querySelector('.page-shutter').getAnimations().every(a => a.playState === 'finished')")
        original_save = page.evaluate("localStorage.getItem('northeast-disco:campaign:v1:auto')")
        for width, height in [(1440, 810), (1920, 1080), (844, 475)]:
            page.set_viewport_size({'width': width, 'height': height})
            page.mouse.move(width-4, height-4)
            metrics = page.evaluate('''() => {
              const rect = e => {const {x,y,width,height}=e.getBoundingClientRect();return {x,y,width,height};};
              const bg=document.querySelector('.event-desk');
              return {background:bg.getAttribute('src'),naturalSize:[bg.naturalWidth,bg.naturalHeight],bounds:rect(bg),
                bag:rect(document.querySelector('.event-dossier-bag')),
                buttons:[...document.querySelectorAll('.mainmenu-choice:not([hidden])')].map(b=>{
                  const r=b.getBoundingClientRect();return {label:b.querySelector('img').alt,
                    reachable:b.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))};})};
            }''')
            assert metrics['background'].lstrip('/') == 'assets/event-desk-bg-medal-marriage-v2.png', metrics
            assert metrics['naturalSize'] == [1671,941], metrics
            assert all(button['reachable'] for button in metrics['buttons']), metrics
            assert page.locator('.view.active').get_attribute('data-view') == 'mainmenu'
            path = ROOT / f'docs/menu-marriage-{width}.png'
            page.screenshot(path=str(path), timeout=10000)
            screens.append({'path':str(path), 'viewport':[width,height], **metrics})
        page.locator('.mainmenu-choice-continue').click()
        page.locator('[data-view="ui-only"].active').wait_for(state='visible')
        page.locator('.ui-only-notice-button').click()
        assert page.locator('.view.active').get_attribute('data-view') == 'mainmenu'
        assert page.evaluate("localStorage.getItem('northeast-disco:campaign:v1:auto')") == original_save
        assert not errors, errors
        report = {'date':'2026-09-13', 'transport':'fixed work Google Chrome + direct CDP 9223', 'browser':browser.version,
                  'storage':'task-created page; isolated memory fixture; real saves untouched',
                  'menuReachable':True, 'continueAndReturn':True, 'savedDataUnchanged':True,
                  'visualAcceptance':'pending user review', 'screens':screens, 'errors':errors}
        (ROOT/'docs/menu-marriage-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        print(json.dumps({'screens':len(screens), 'menuReachable':True, 'continueAndReturn':True, 'errors':errors}))
    finally:
        page.close()
