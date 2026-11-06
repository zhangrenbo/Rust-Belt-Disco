"""Visual and interaction checks in a disposable, background work-Chrome CDP page."""
import argparse
import json
import os
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
os.environ['NO_PROXY'] = 'localhost,127.0.0.1'
parser = argparse.ArgumentParser()
parser.add_argument('--width', type=int, default=1440)
args = parser.parse_args()
width, height = args.width, round(args.width * 9 / 16)
checks, errors = [], []

with sync_playwright() as pw:
    browser = pw.chromium.connect_over_cdp('http://127.0.0.1:9223', timeout=10000)
    context = browser.contexts[0]
    bc = browser.new_browser_cdp_session()
    with context.expect_page(timeout=10000) as created:
        bc.send('Target.createTarget', {'url': 'about:blank', 'background': True})
    page = created.value
    try:
        page.set_default_timeout(10000)
        page.route('**/@vite/client', lambda r: r.fulfill(status=200, content_type='application/javascript', body=''))
        page.add_init_script('''(() => {
          const values = new Map([['northeast-disco.settings.v1', '{"music":0,"film":true,"filmStrength":65,"filmMotion":true}']]);
          Object.defineProperty(window,'localStorage',{value:{
            getItem:k=>values.get(String(k))??null,setItem:(k,v)=>values.set(String(k),String(v)),
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
        page.emulate_media(reduced_motion='no-preference')
        page.set_viewport_size({'width': width, 'height': height})
        page.goto(f'http://127.0.0.1:5173/?settings-light-qa={time.time_ns()}', wait_until='domcontentloaded')
        page.wait_for_function('document.querySelector("[data-view=mainmenu].active") && document.querySelector("#app").dataset.film === "on"')
        menu_film = page.evaluate('''() => ({...document.querySelector('#app').dataset,
          filter:getComputedStyle(document.querySelector('.projector-film')).backdropFilter})''')
        page.locator('.mainmenu-actions [data-open-view=settings]').click()
        page.wait_for_function('''[...document.querySelectorAll('.view.active img')].filter(i=>i.getAttribute('src')).every(i=>i.complete&&i.naturalWidth>0) && getComputedStyle(document.querySelector('.view.active')).opacity==='1' ''')
        page.wait_for_function('''getComputedStyle(document.querySelector('.settings-paper-tabs'),'::after').backgroundImage.includes('v5-no-key')''')
        layout = page.evaluate('''() => {
          const view=document.querySelector('.view-settings'), stage=document.querySelector('#app');
          const seam=getComputedStyle(document.querySelector('.settings-paper-tabs'),'::after');
          const lights=['::before','::after'].map(p=>{const c=getComputedStyle(view,p);return {display:c.display,z:c.zIndex,pointer:c.pointerEvents,blend:c.mixBlendMode,background:c.backgroundImage}});
          const buttons=[...view.querySelectorAll('button,input')].filter(e=>e.getClientRects().length&&!e.disabled).map(e=>{
            const r=e.getBoundingClientRect();return {name:e.textContent.trim()||e.getAttribute('aria-label'),hit:e.contains(document.elementFromPoint(r.x+r.width/2,r.y+r.height/2))};
          });
          return {lights,buttons,occlusion:{width:seam.width,height:seam.height,left:seam.left,top:seam.top},
            film:stage.dataset.film,motion:stage.dataset.filmMotion,
            filter:getComputedStyle(document.querySelector('.projector-film')).backdropFilter,
            grainOpacity:getComputedStyle(document.querySelector('.grain')).opacity,
            footerBackgrounds:[...view.querySelectorAll('.settings-board-actions button')].map(e=>getComputedStyle(e).backgroundColor)};
        }''')
        assert all(b['hit'] for b in layout['buttons']), layout['buttons']
        assert all(l['pointer']=='none' and l['display']!='none' for l in layout['lights']), layout['lights']
        assert layout['filter']==menu_film['filter'] and layout['film']=='on' and layout['motion']=='on', layout
        assert all(b=='rgba(0, 0, 0, 0)' for b in layout['footerBackgrounds'])
        checks += ['all visible controls reachable', 'light layers do not intercept input', 'same film pass as main menu', 'bottom actions keep transparent backgrounds']
        page.mouse.move(width-2,height-2)
        for w in [width+1,width]:
            cdp.send('Emulation.setDeviceMetricsOverride', {'width':w,'height':height,'deviceScaleFactor':1,'mobile':False})
        screenshot = ROOT/f'docs/settings-daylight-{width}.png'
        page.screenshot(path=str(screenshot), timeout=12000)
        print('Captured', screenshot.name, flush=True)

        samples = []
        end = time.monotonic()+8
        while time.monotonic()<end:
            samples.append(page.evaluate('''() => {
              const art=document.querySelector('.settings-dossier-art'),r=art.getBoundingClientRect(),c=getComputedStyle(art);
              const heading=document.querySelector('#settings-title').getBoundingClientRect();
              return {x:r.x,y:r.y,headingX:heading.x,headingY:heading.y,translate:c.translate,
                grain:getComputedStyle(document.querySelector('.grain')).backgroundPosition,
                dust:document.querySelectorAll('.film-dust:not([hidden]),.film-fiber:not([hidden])').length,
                scratches:document.querySelectorAll('.film-scratch:not([hidden])').length};
            }'''))
            time.sleep(.07)
        motion = {'grainFrames':len(set(s['grain'] for s in samples)),
                  'dustFrames':sum(s['dust']>0 for s in samples),
                  'scratchFrames':sum(s['scratches']>0 for s in samples),
                  'artAndTextStable':all(len(set(s[k] for s in samples))==1 for k in ['x','y','headingX','headingY']) and all(s['translate']=='none' for s in samples)}
        assert motion['grainFrames']>4 and motion['dustFrames']>0 and motion['scratchFrames']>0 and motion['artAndTextStable'], motion
        checks += ['grain moves; dust and scratches appear', 'artwork and text remain stable']

        page.locator('[data-music-library-open]').click()
        page.wait_for_function('document.querySelector(".view-settings").classList.contains("is-record-library")')
        assert page.locator('.view-settings').evaluate("e=>getComputedStyle(e,'::before').display==='none' && getComputedStyle(e,'::after').content==='none'")
        page.locator('[data-music-library-close]').click()
        page.locator('[data-personnel-open]').click()
        page.wait_for_function('document.querySelector("[data-view=explorer-dossier].active")')
        page.keyboard.press('Escape')
        page.wait_for_function('document.querySelector("[data-view=settings].active")')
        checks += ['music library opens and returns without dossier lighting', 'personnel dossier opens and Escape returns']
        page.locator('[data-film-toggle]').click()
        assert page.locator('#app').get_attribute('data-film')=='off'
        assert page.locator('.grain').evaluate("e=>getComputedStyle(e).display==='none'")
        page.locator('[data-film-toggle]').click()
        page.locator('button[data-film-motion]').click()
        assert page.locator('#app').get_attribute('data-film-motion')=='off'
        page.locator('button[data-film-motion]').click()
        page.locator('[data-settings-return]').click()
        page.wait_for_function('document.querySelector("[data-view=mainmenu].active")')
        checks += ['film and motion switches still work', 'Return goes to main menu']
        assert not errors, errors
        (ROOT/f'docs/settings-daylight-{width}-verification.json').write_text(json.dumps({
            'transport':'fixed work Chrome, direct CDP 9223, task-created background target',
            'storage':'isolated memory; user data untouched',
            'viewport':[width,height], 'screenshot':str(screenshot),'menuFilm':menu_film,
            'layout':layout,'motion':motion,'checks':checks,'errors':errors,'humanArtReview':'pending'
        },ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'checks':len(checks),'motion':motion,'errors':errors}),flush=True)
    finally:
        page.close()
