"""Verify the public UI-only build in a task page with isolated memory saves."""
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
    session = context.new_cdp_session(page)
    assert session.send('Target.getTargetInfo')['targetInfo']['targetId'] == target
    session.send('Emulation.setFocusEmulationEnabled', {'enabled': True})
    try:
        session.send('Page.setWebLifecycleState', {'state': 'active'})
    except Exception as error:
        print('Lifecycle activation:', str(error)[:160])
    errors, checks, screens = [], [], []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.add_init_script('''(() => {
      const save = JSON.stringify({version:1,character:'dong-dianshi',scene:'A000',node:'arrival',answers:{},inspected:[],
        position:{x:440,y:640},warningHeard:false,shadow:null,completed:false,updatedAt:'2026-09-13T00:00:00.000Z'});
      const values = new Map([['northeast-disco:campaign:v1:auto',save],['northeast-disco:campaign:v1:01',save],
        ['northeast-disco:campaign:v1:04','damaged save'],['qa-unrelated','preserved']]);
      Object.defineProperty(window,'localStorage',{configurable:true,value:{getItem:k=>values.get(String(k))??null,
        setItem:(k,v)=>values.set(String(k),String(v)),removeItem:k=>values.delete(String(k)),clear:()=>values.clear(),
        key:i=>[...values.keys()][i]??null,get length(){return values.size;}}});
    })();''')
    def check(name, value):
        assert value, name
        checks.append(name)
    def snapshot():
        return page.evaluate('Object.fromEntries(Array.from({length:localStorage.length},(_,i)=>{const k=localStorage.key(i);return [k,localStorage.getItem(k)];}))')
    def notice():
        page.wait_for_function('document.querySelector("#ui-only-notice").classList.contains("active")', timeout=5000)
    def capture(width, height):
        page.set_viewport_size({'width': width, 'height': height})
        notice()
        metrics = page.locator('#ui-only-notice').evaluate('''d => {
          const b=d.querySelector('button'),r=d.getBoundingClientRect(),br=b.getBoundingClientRect();
          const range=document.createRange();range.selectNodeContents(d.querySelector('p'));const t=range.getBoundingClientRect();
          return {background:getComputedStyle(d).backgroundColor,buttonBackground:getComputedStyle(b).backgroundColor,
            buttonArt:getComputedStyle(b).backgroundImage,buttonFont:getComputedStyle(b).fontFamily,buttonInk:getComputedStyle(b).color,
            width:r.width,height:r.height,text:d.querySelector('p').textContent,textX:t.x,textY:t.y,textWidth:t.width,textHeight:t.height,
            buttonText:b.textContent,buttonVisible:br.width>0&&br.height>=44&&br.y>t.bottom&&br.bottom<innerHeight&&b.contains(document.elementFromPoint(br.x+br.width/2,br.y+br.height/2)),
            corners:[[1,1],[innerWidth-2,1],[1,innerHeight-2],[innerWidth-2,innerHeight-2]].every(([x,y])=>d.contains(document.elementFromPoint(x,y)))};
        }''')
        check(f'pure black fills {width}x{height}', metrics['background']=='rgb(0, 0, 0)' and metrics['width']==width and metrics['height']==height and metrics['corners'])
        check(f'exact horizontally centered notice at {width}', metrics['text']=='仅UI展示，无可游玩内容' and abs(metrics['textX']+metrics['textWidth']/2-width/2)<1)
        check(f'visible return button below the notice at {width}', metrics['buttonText']=='返回主菜单' and metrics['buttonVisible'])
        path=ROOT/f'docs/ui-only-notice-{width}.png'
        page.screenshot(path=str(path),timeout=10000)
        screens.append({'path':str(path),'viewport':[width,height],**metrics})
    try:
        page.set_viewport_size({'width':1440,'height':810})
        page.goto(f'http://127.0.0.1:5173/?ui-only-qa={time.time_ns()}',wait_until='domcontentloaded')
        page.locator('.mainmenu-choice-continue').wait_for(state='visible',timeout=10000)
        check('opening the application shows the homepage without a notice', page.locator('.view.active').get_attribute('data-view')=='mainmenu' and not page.locator('#ui-only-notice').is_visible())
        page.wait_for_function("() => !document.querySelector('#app').classList.contains('is-view-switching') && document.querySelector('.page-shutter').getAnimations().every(a => a.playState === 'finished')")
        page.wait_for_function("() => [...document.querySelectorAll('.view-mainmenu img')].every(i => i.complete && i.naturalWidth > 0)")
        page.screenshot(path=str(ROOT/'docs/ui-only-homepage-1440.png'),timeout=10000)
        saved=snapshot()
        page.locator('.mainmenu-choice-start').click()
        check('Start only opens character selection without a notice', page.locator('.view.active').get_attribute('data-view')=='character-select' and not page.locator('#ui-only-notice').is_visible())
        page.locator('[data-character-choice="大厦崩塌"]').click(timeout=10000)
        notice()
        check('confirming a new game enters a separate placeholder scene', page.locator('.view.active').get_attribute('data-view')=='ui-only' and page.locator('[data-view=character-select]').get_attribute('aria-hidden')=='true')
        for width,height in [(1440,810),(844,475),(390,844)]:
            capture(width,height)
        page.set_viewport_size({'width':1440,'height':810})
        page.keyboard.press('Enter')
        check('Enter activates the focused return button and returns to the homepage', page.locator('.view.active').get_attribute('data-view')=='mainmenu' and not page.locator('#ui-only-notice').is_visible())
        page.locator('.mainmenu-choice-continue').click(timeout=10000)
        notice()
        check('continue enters the placeholder scene', page.locator('.view.active').get_attribute('data-view')=='ui-only')
        page.locator('#ui-only-notice button').click()
        check('return button exits continued game to the homepage', page.locator('.view.active').get_attribute('data-view')=='mainmenu')
        page.locator('.mainmenu-choice-start').click()
        page.locator('[data-character-choice="大厦崩塌"]').click(timeout=10000)
        notice()
        page.keyboard.press('Escape')
        check('Escape exits the new-game placeholder scene to the homepage', page.locator('.view.active').get_attribute('data-view')=='mainmenu')
        page.locator('.mainmenu-actions [data-open-view="entry"]').click()
        page.locator('[data-view=entry] [data-slot="01"]').click()
        check('saved-slot writing is disabled', page.locator('.station-slot-actions button').nth(1).is_disabled())
        page.locator('.station-slot-actions button').first.click()
        notice()
        check('load closes the slot dialog and enters the placeholder scene', not page.locator('#slot-dialog').evaluate('(d)=>d.open') and page.locator('.view.active').get_attribute('data-view')=='ui-only')
        page.locator('#ui-only-notice button').click()
        check('return button exits loaded game to the homepage', page.locator('.view.active').get_attribute('data-view')=='mainmenu')
        page.locator('.mainmenu-actions [data-open-view="settings"]').click()
        page.locator('[data-personnel-open]').click()
        check('personnel UI remains available', page.locator('[data-view=explorer-dossier]').is_visible())
        page.locator('[data-dossier-return]').click()
        page.locator('[data-settings-return]').click()
        check('settings returns to the menu', page.locator('.view.active').get_attribute('data-view')=='mainmenu')
        check('all saved data is unchanged', snapshot()==saved)
        check('station neither initialized nor loaded its background', page.locator('[data-view=station]').get_attribute('data-story-node') is None and page.locator('#station-world .station-background').get_attribute('src') is None)
        page.goto(f'http://127.0.0.1:5173/?ui-only-direct-qa={time.time_ns()}#station',wait_until='domcontentloaded')
        notice()
        check('a direct gameplay URL also enters the placeholder scene', page.locator('.view.active').get_attribute('data-view')=='ui-only')
        check('no runtime errors', not errors)
        report={'date':'2026-09-13','transport':'fixed work Google Chrome + direct CDP 9223','browser':browser.version,
                'storage':'task-created page; in-memory fixtures; real saves untouched','checks':checks,'screens':screens,'errors':errors}
        (ROOT/'docs/ui-only-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        print(json.dumps({'checks':len(checks),'screens':len(screens),'errors':errors}))
    finally:
        page.close()
